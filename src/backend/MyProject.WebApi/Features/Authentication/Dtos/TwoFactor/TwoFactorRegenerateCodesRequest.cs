using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace MyProject.WebApi.Features.Authentication.Dtos.TwoFactor;

/// <summary>
/// Request to regenerate two-factor recovery codes.
/// </summary>
public class TwoFactorRegenerateCodesRequest
{
    /// <summary>
    /// The user's current password for confirmation.
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string Password { get; [UsedImplicitly] init; } = string.Empty;
}
