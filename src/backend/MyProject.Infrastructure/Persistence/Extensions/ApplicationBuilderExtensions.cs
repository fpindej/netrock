using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
            ApplyMigrations(services);
        }

        await SeedRolesAsync(services);
        await SeedRolePermissionsAsync(services);
        await SeedUsersFromConfigurationAsync(services);
    }

    private static void ApplyMigrations(IServiceProvider serviceProvider)
    {
        var dbContext = serviceProvider.GetRequiredService<MyProjectDbContext>();
        dbContext.Database.Migrate();
    }

    private static async Task SeedRolesAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new ApplicationRole { Name = role });
            }
        }
    }

    /// <summary>
    /// Seeds the default permission claims for the Admin role.
    /// Idempotent — skips permissions that already exist as role claims.
    /// SuperAdmin is not seeded because it has implicit all permissions.
    /// </summary>
    private static async Task SeedRolePermissionsAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        // Admin gets user management + role viewing by default.
        // Roles.Manage is deliberately excluded — only SuperAdmin can create/edit/delete roles.
        var adminPermissions = new[]
        {
            AppPermissions.Users.View,
            AppPermissions.Users.Manage,
            AppPermissions.Users.AssignRoles,
            AppPermissions.Roles.View
        };

        var adminRole = await roleManager.FindByNameAsync(AppRoles.Admin);
        if (adminRole is null) return;

        var existingClaims = await roleManager.GetClaimsAsync(adminRole);
        var existingPermissions = existingClaims
            .Where(c => c.Type == AppPermissions.ClaimType)
            .Select(c => c.Value)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var permission in adminPermissions)
        {
            if (!existingPermissions.Contains(permission))
            {
                await roleManager.AddClaimAsync(adminRole, new Claim(AppPermissions.ClaimType, permission));
            }
        }
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

            await SeedUserAsync(userManager, entry.Email, entry.Password, role);
            Log.Information("Seed: ensured user {Email} with role {Role}", entry.Email, role);
        }
    }

    private static async Task SeedUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        string role)
    {
        if (await userManager.FindByNameAsync(email) is not null)
        {
            return;
        }

        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        await userManager.CreateAsync(user, password);
        await userManager.AddToRoleAsync(user, role);
    }
}
