using Microsoft.AspNetCore.Identity;

namespace MyProject.Infrastructure.Features.Authentication.Models;

/// <summary>
/// Application-specific Identity role with <see cref="Guid"/> as the key type.
/// </summary>
public class ApplicationRole : IdentityRole<Guid>
{
    /// <summary>
    /// An optional human-readable description of the role's purpose.
    /// </summary>
    public string? Description { get; set; }
}
