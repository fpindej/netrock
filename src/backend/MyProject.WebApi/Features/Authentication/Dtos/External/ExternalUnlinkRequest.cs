using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace MyProject.WebApi.Features.Authentication.Dtos.External;

/// <summary>
/// Request to unlink an external provider from the current user's account.
/// </summary>
public class ExternalUnlinkRequest
{
    /// <summary>
    /// The provider name to unlink (e.g. "Google", "GitHub").
    /// </summary>
    [Required]
    [MaxLength(32)]
    public string Provider { get; [UsedImplicitly] init; } = string.Empty;
}
