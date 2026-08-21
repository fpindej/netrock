using MyProject.Application.Identity.Constants;
using MyProject.Infrastructure.Features.Authentication.Models;

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
}
