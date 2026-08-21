using Microsoft.AspNetCore.Identity;

namespace MyProject.Infrastructure.Features.Authentication.Models;

/// <summary>
/// Application-specific Identity role with <see cref="Guid"/> as the key type.
/// <para>
/// Multitenancy seam: tenant-scoped roles would add a nullable <c>TenantId</c> here.
/// </para>
/// </summary>
public class ApplicationRole : IdentityRole<Guid>
{
    /// <summary>
    /// An optional human-readable description of the role's purpose.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether the role is a built-in system role that cannot be renamed or deleted.
    /// </summary>
    public bool IsSystem { get; set; }

    /// <summary>
    /// The hierarchy rank. Higher rank means more authority; custom roles have rank 0.
    /// </summary>
    public int Rank { get; set; }

    /// <summary>
    /// Whether the role implicitly grants every permission. Such roles receive a single
    /// wildcard permission claim in the JWT and their permission list cannot be edited.
    /// </summary>
    public bool GrantsAllPermissions { get; set; }
}
