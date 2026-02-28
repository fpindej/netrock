using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace MyProject.WebApi.Features.Authentication.Dtos.TwoFactor;

/// <summary>
/// Request to complete a two-factor login with a TOTP code.
/// </summary>
public class TwoFactorLoginRequest
{
    /// <summary>
    /// The opaque challenge token received from the initial login response.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ChallengeToken { get; [UsedImplicitly] init; } = string.Empty;

    /// <summary>
    /// The 6-digit TOTP code from the authenticator app.
    /// </summary>
    [Required]
    [MaxLength(6)]
    public string Code { get; [UsedImplicitly] init; } = string.Empty;
}
