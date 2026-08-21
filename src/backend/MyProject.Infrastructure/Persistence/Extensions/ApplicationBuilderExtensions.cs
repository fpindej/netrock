using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyProject.Application.Caching.Constants;
using MyProject.Application.Identity.Constants;
using MyProject.Infrastructure.Features.Authentication.Models;
using MyProject.Infrastructure.Persistence.Options;
using Serilog;

namespace MyProject.Infrastructure.Persistence.Extensions;

/// <summary>
/// Extension methods for database initialization at startup — migrations, role seeding, and user seeding.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Initializes the database: applies migrations (development only), seeds roles (always),
    /// and seeds users from configuration (always — env vars gate whether any users are seeded).
    /// </summary>
    /// <param name="appBuilder">The application builder.</param>
    public static async Task InitializeDatabaseAsync(this IApplicationBuilder appBuilder)
    {
        using var scope = appBuilder.ApplicationServices.CreateScope();
        var services = scope.ServiceProvider;
        var isDevelopment = services.GetRequiredService<IHostEnvironment>().IsDevelopment();

        if (isDevelopment)
        {
            await ApplyMigrationsAsync(services);
        }

        await SeedRolesAsync(services);
        await SeedUsersFromConfigurationAsync(services);
    }

    private static async Task ApplyMigrationsAsync(IServiceProvider serviceProvider)
    {
        var dbContext = serviceProvider.GetRequiredService<MyProjectDbContext>();
        await WaitForDatabaseAsync(dbContext);
        await dbContext.Database.MigrateAsync();
    }

    /// <summary>
    /// Blocks until PostgreSQL accepts connections. On first Aspire launch the container
    /// may report healthy before the server is fully ready. <see cref="DatabaseFacade.CanConnectAsync"/>
    /// returns <c>false</c> without logging errors, unlike <see cref="RelationalDatabaseFacadeExtensions.MigrateAsync"/>
    /// which logs at Error level on transient failures.
    /// </summary>
    private static async Task WaitForDatabaseAsync(MyProjectDbContext dbContext)
    {
        const int maxAttempts = 30;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            if (await dbContext.Database.CanConnectAsync())
            {
                return;
            }

            Log.Information("Waiting for database to accept connections (attempt {Attempt}/{MaxAttempts})",
                attempt, maxAttempts);
            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        throw new InvalidOperationException(
            $"Database did not become available after {maxAttempts} attempts ({maxAttempts * 2}s).");
    }

    /// <summary>
    /// Upserts the built-in roles from <see cref="AppRoles.Definitions"/>: creates missing roles
    /// with their metadata, updates existing roles only when metadata drifted (no writes on a
    /// converged database), and additively seeds missing default permission claims so operator
    /// customizations are preserved.
    /// </summary>
    private static async Task SeedRolesAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        foreach (var definition in AppRoles.Definitions)
        {
            var role = await roleManager.FindByNameAsync(definition.Name);

            if (role is null)
            {
                role = new ApplicationRole
                {
                    Name = definition.Name,
                    Description = definition.Description,
                    IsSystem = definition.IsSystem,
                    Rank = definition.Rank,
                    GrantsAllPermissions = definition.GrantsAllPermissions
                };

                var createResult = await roleManager.CreateAsync(role);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                    Log.Error("Seed: failed to create role {RoleName}: {Errors}", definition.Name, errors);
                    continue;
                }

                Log.Information("Seed: created role {RoleName}", definition.Name);
            }
            else
            {
                await UpdateRoleMetadataIfDriftedAsync(serviceProvider, roleManager, role, definition);
            }

            await SeedDefaultPermissionsAsync(roleManager, role, definition);
        }
    }

    /// <summary>
    /// Updates a role's metadata columns when they differ from the declarative definition.
    /// When <c>GrantsAllPermissions</c> transitions from false to true (first run against a
    /// pre-refactor database), security stamps of users in the role are rotated so their old
    /// access tokens (which lack the wildcard permission claim) are invalidated; the frontend
    /// then silently refreshes and receives a wildcard token.
    /// </summary>
    private static async Task UpdateRoleMetadataIfDriftedAsync(
        IServiceProvider serviceProvider,
        RoleManager<ApplicationRole> roleManager,
        ApplicationRole role,
        RoleDefinition definition)
    {
        var grantsAllTransitioned = !role.GrantsAllPermissions && definition.GrantsAllPermissions;

        var drifted = role.Description != definition.Description
                      || role.IsSystem != definition.IsSystem
                      || role.Rank != definition.Rank
                      || role.GrantsAllPermissions != definition.GrantsAllPermissions;

        if (!drifted)
        {
            return;
        }

        role.Description = definition.Description;
        role.IsSystem = definition.IsSystem;
        role.Rank = definition.Rank;
        role.GrantsAllPermissions = definition.GrantsAllPermissions;

        var result = await roleManager.UpdateAsync(role);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            Log.Error("Seed: failed to update metadata for role {RoleName}: {Errors}", definition.Name, errors);
            return;
        }

        Log.Information("Seed: updated metadata for role {RoleName}", definition.Name);

        if (grantsAllTransitioned)
        {
            await RotateSecurityStampsForRoleAsync(serviceProvider, role.Id);
        }
    }

    /// <summary>
    /// Adds missing default permission claims for a role. Additive-only so permissions granted
    /// by operators at runtime are never removed by seeding.
    /// </summary>
    private static async Task SeedDefaultPermissionsAsync(
        RoleManager<ApplicationRole> roleManager,
        ApplicationRole role,
        RoleDefinition definition)
    {
        if (definition.DefaultPermissions.Count == 0)
        {
            return;
        }

        var existingClaims = await roleManager.GetClaimsAsync(role);
        var existingPermissions = existingClaims
            .Where(c => c.Type == AppPermissions.ClaimType)
            .Select(c => c.Value)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var permission in definition.DefaultPermissions)
        {
            if (!existingPermissions.Contains(permission))
            {
                await roleManager.AddClaimAsync(role, new Claim(AppPermissions.ClaimType, permission));
            }
        }
    }

    /// <summary>
    /// Rotates security stamps for all users in a role, invalidating their current access tokens.
    /// Refresh tokens are intentionally preserved so the frontend can silently re-authenticate
    /// and obtain a new JWT with updated permission claims.
    /// </summary>
    private static async Task RotateSecurityStampsForRoleAsync(IServiceProvider serviceProvider, Guid roleId)
    {
        var dbContext = serviceProvider.GetRequiredService<MyProjectDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var hybridCache = serviceProvider.GetRequiredService<HybridCache>();

        var userIds = await dbContext.UserRoles
            .Where(ur => ur.RoleId == roleId)
            .Select(ur => ur.UserId)
            .ToListAsync();

        if (userIds.Count == 0) return;

        var users = await dbContext.Users
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync();

        foreach (var user in users)
        {
            await userManager.UpdateSecurityStampAsync(user);
            await hybridCache.RemoveAsync(CacheKeys.SecurityStamp(user.Id));
            await hybridCache.RemoveAsync(CacheKeys.User(user.Id));
        }

        Log.Information("Seed: rotated security stamps for {UserCount} user(s) in role '{RoleId}'",
            users.Count, roleId);
    }

    /// <summary>
    /// Seeds users from the <c>Seed:Users</c> configuration section.
    /// Each entry must have a non-empty Email, Password, and a valid Role.
    /// Incomplete or invalid entries are logged as warnings and skipped.
    /// Idempotent — existing users (matched by email) are not modified.
    /// </summary>
    private static async Task SeedUsersFromConfigurationAsync(IServiceProvider serviceProvider)
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var seedOptions = configuration.GetSection(SeedOptions.SectionName).Get<SeedOptions>();

        if (seedOptions?.Users is not { Count: > 0 })
        {
            return;
        }

        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var validRoles = AppRoles.All.ToHashSet(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < seedOptions.Users.Count; i++)
        {
            var entry = seedOptions.Users[i];

            if (string.IsNullOrWhiteSpace(entry.Email) || string.IsNullOrWhiteSpace(entry.Password))
            {
                Log.Warning("Seed:Users[{Index}] is missing Email or Password — skipping", i);
                continue;
            }

            if (!validRoles.Contains(entry.Role))
            {
                Log.Warning(
                    "Seed:Users[{Index}] has invalid role '{Role}' — skipping. Valid roles: {ValidRoles}",
                    i, entry.Role, string.Join(", ", AppRoles.All));
                continue;
            }

            // Resolve the exact role name (case-insensitive match → canonical casing).
            var role = AppRoles.All.First(r => string.Equals(r, entry.Role, StringComparison.OrdinalIgnoreCase));

            await SeedUserAsync(userManager, entry.Email, entry.Password, role, i);
        }
    }

    private static async Task SeedUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        string role,
        int index)
    {
        if (await userManager.FindByNameAsync(email) is not null)
        {
            Log.Debug("Seed:Users[{Index}] {Email} already exists — skipping", index, email);
            return;
        }

        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        var result = await userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            Log.Error("Seed:Users[{Index}] failed to create {Email}: {Errors}", index, email, errors);
            return;
        }

        await userManager.AddToRoleAsync(user, role);
        Log.Information("Seed: created user {Email} with role {Role}", email, role);
    }
}
