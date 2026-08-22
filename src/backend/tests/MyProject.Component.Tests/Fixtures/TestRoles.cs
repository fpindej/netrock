using Microsoft.AspNetCore.Identity;
using MyProject.Application.Identity.Constants;
using MyProject.Infrastructure.Features.Authentication.Models;
using MyProject.Infrastructure.Persistence;

namespace MyProject.Component.Tests.Fixtures;

/// <summary>
/// Builds <see cref="ApplicationRole"/> instances for test fixtures.
/// System roles copy their metadata (rank, flags, description) from
/// <see cref="AppRoles.Definitions"/> so fixtures can never drift from the declarative source.
/// Unknown names produce a custom role with rank 0 and no flags.
/// </summary>
internal static class TestRoles
{
    /// <summary>
    /// Creates a role named <paramref name="name"/> with metadata taken from
    /// <see cref="AppRoles.Definitions"/> when the name matches a system role.
    /// </summary>
    /// <param name="name">The role name.</param>
    /// <param name="id">An optional fixed role id; a new one is generated when omitted.</param>
    public static ApplicationRole Create(string name, Guid? id = null)
    {
        var definition = AppRoles.Definitions.SingleOrDefault(d => d.Name == name);

        return new ApplicationRole
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            NormalizedName = name.ToUpperInvariant(),
            Description = definition?.Description,
            IsSystem = definition?.IsSystem ?? false,
            Rank = definition?.Rank ?? 0,
            GrantsAllPermissions = definition?.GrantsAllPermissions ?? false
        };
    }

    /// <summary>
    /// Creates a role via <see cref="Create(string, Guid?)"/> and persists it to the given
    /// InMemory context so services that read role metadata from the database can resolve it.
    /// </summary>
    /// <param name="dbContext">The test database context.</param>
    /// <param name="name">The role name.</param>
    /// <param name="id">An optional fixed role id; a new one is generated when omitted.</param>
    public static ApplicationRole Seed(MyProjectDbContext dbContext, string name, Guid? id = null)
    {
        var role = Create(name, id);
        dbContext.Roles.Add(role);
        dbContext.SaveChanges();
        return role;
    }

    /// <summary>
    /// Seeds a role, assigns it to the given user, and optionally attaches permission claims,
    /// so database-driven rank, escalation, and lockout checks can resolve the assignment.
    /// </summary>
    /// <param name="dbContext">The test database context.</param>
    /// <param name="userId">The user the role is assigned to.</param>
    /// <param name="name">The role name.</param>
    /// <param name="permissions">Permission claim values granted by the role.</param>
    public static ApplicationRole SeedAssigned(MyProjectDbContext dbContext, Guid userId, string name,
        params string[] permissions)
    {
        var role = Create(name);
        dbContext.Roles.Add(role);
        dbContext.UserRoles.Add(new IdentityUserRole<Guid> { UserId = userId, RoleId = role.Id });

        foreach (var permission in permissions)
        {
            dbContext.RoleClaims.Add(new IdentityRoleClaim<Guid>
            {
                RoleId = role.Id,
                ClaimType = AppPermissions.ClaimType,
                ClaimValue = permission
            });
        }

        dbContext.SaveChanges();
        return role;
    }
}
