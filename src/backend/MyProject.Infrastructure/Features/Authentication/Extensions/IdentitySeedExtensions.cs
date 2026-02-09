using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using MyProject.Application.Identity.Constants;
using MyProject.Infrastructure.Features.Authentication.Constants;
using MyProject.Infrastructure.Features.Authentication.Models;

namespace MyProject.Infrastructure.Features.Authentication.Extensions;

/// <summary>
/// Internal methods for seeding Identity roles and development test users.
/// Called by the database initialization orchestrator, not directly from <c>Program.cs</c>.
/// </summary>
internal static class IdentitySeedExtensions
{
    /// <summary>
    /// Seeds all roles defined in <see cref="AppRoles.All"/> if they do not already exist.
    /// </summary>
    /// <param name="serviceProvider">The scoped service provider.</param>
    internal static async Task SeedRolesAsync(IServiceProvider serviceProvider)
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
    /// Seeds test users for local development.
    /// Roles must already exist — call <see cref="SeedRolesAsync"/> first.
    /// </summary>
    /// <param name="serviceProvider">The scoped service provider.</param>
    internal static async Task SeedDevelopmentUsersAsync(IServiceProvider serviceProvider)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await SeedUserAsync(userManager, SeedUsers.TestUserEmail, SeedUsers.TestUserPassword, AppRoles.User);
        await SeedUserAsync(userManager, SeedUsers.AdminEmail, SeedUsers.AdminPassword, AppRoles.Admin);
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
