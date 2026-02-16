using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace MyProject.WebApi.Features.Authentication.Dtos.ForgotPassword;

/// <summary>
/// Represents a request to initiate a password reset flow.
/// </summary>
[UsedImplicitly]
public class ForgotPasswordRequest
{
    /// <summary>
    /// The email address associated with the account.
    /// </summary>
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; [UsedImplicitly] init; } = string.Empty;

    /// <summary>
    /// The CAPTCHA verification token from Cloudflare Turnstile.
    /// </summary>
    [Required]
    public string CaptchaToken { get; [UsedImplicitly] init; } = string.Empty;
}
