using System.ComponentModel.DataAnnotations;

namespace MyProject.Infrastructure.Features.Captcha.Options;

/// <summary>
/// Configuration options for Cloudflare Turnstile CAPTCHA verification.
/// </summary>
public sealed class CaptchaOptions
{
    /// <summary>
    /// The configuration section name in appsettings.
    /// </summary>
    public const string SectionName = "Captcha";

    /// <summary>
    /// The Turnstile secret key (private, used for server-side verification).
    /// </summary>
    [Required]
    public string SecretKey { get; init; } = string.Empty;

    /// <summary>
    /// The URL for Cloudflare Turnstile's server-side verification endpoint.
    /// </summary>
    public string VerifyUrl { get; init; } = "https://challenges.cloudflare.com/turnstile/v0/siteverify";
}
