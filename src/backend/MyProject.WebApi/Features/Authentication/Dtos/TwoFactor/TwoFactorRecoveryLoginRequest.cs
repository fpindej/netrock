using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace MyProject.WebApi.Features.Authentication.Dtos.TwoFactor;

/// <summary>
/// Request to complete a two-factor login with a recovery code.
/// </summary>
public class TwoFactorRecoveryLoginRequest
{
    /// <summary>
    /// The opaque challenge token received from the initial login response.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ChallengeToken { get; [UsedImplicitly] init; } = string.Empty;

    /// <summary>
    /// The one-time recovery code.
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string RecoveryCode { get; [UsedImplicitly] init; } = string.Empty;
}
