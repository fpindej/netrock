using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using MyProject.Application.Identity.Constants;
using MyProject.Infrastructure.Features.Authentication.Constants;
using MyProject.Infrastructure.Features.Authentication.Models;

namespace MyProject.Infrastructure.Features.Authentication.Extensions;

/// <summary>
/// Extension methods for seeding default Identity users and roles at startup.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Seeds default roles and test users if they do not already exist.
    /// <para>
    /// Roles are defined in <see cref="AppRoles"/> — this method ensures all roles from
    /// <see cref="AppRoles.All"/> exist, then creates test users for development.
    /// </para>
    /// </summary>
    /// <param name="appBuilder">The application builder.</param>
    public static async Task SeedIdentityUsersAsync(this IApplicationBuilder appBuilder)
    {
        using var scope = appBuilder.ApplicationServices.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new ApplicationRole { Name = role });
            }
        }

        var testUser = await userManager.FindByNameAsync(SeedUsers.TestUserEmail);
        if (testUser is null)
        {
            testUser = new ApplicationUser { UserName = SeedUsers.TestUserEmail, Email = SeedUsers.TestUserEmail, EmailConfirmed = true };
            await userManager.CreateAsync(testUser, SeedUsers.TestUserPassword);
            await userManager.AddToRoleAsync(testUser, AppRoles.User);
        }

        var adminUser = await userManager.FindByNameAsync(SeedUsers.AdminEmail);
        if (adminUser is null)
        {
            adminUser = new ApplicationUser { UserName = SeedUsers.AdminEmail, Email = SeedUsers.AdminEmail, EmailConfirmed = true };
            await userManager.CreateAsync(adminUser, SeedUsers.AdminPassword);
            await userManager.AddToRoleAsync(adminUser, AppRoles.Admin);
        }
    }
}
