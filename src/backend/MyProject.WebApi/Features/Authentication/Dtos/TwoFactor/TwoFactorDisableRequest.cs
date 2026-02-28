using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace MyProject.WebApi.Features.Authentication.Dtos.TwoFactor;

/// <summary>
/// Request to disable two-factor authentication for the current user.
/// </summary>
public class TwoFactorDisableRequest
{
    /// <summary>
    /// The user's current password for confirmation.
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string Password { get; [UsedImplicitly] init; } = string.Empty;
}
