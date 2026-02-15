using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace MyProject.WebApi.Features.Authentication.Dtos.VerifyEmail;

/// <summary>
/// Represents a request to verify an email address using a confirmation token.
/// </summary>
[UsedImplicitly]
public class VerifyEmailRequest
{
    /// <summary>
    /// The email address to verify.
    /// </summary>
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; [UsedImplicitly] init; } = string.Empty;

    /// <summary>
    /// The email confirmation token received via email.
    /// </summary>
    [Required]
    public string Token { get; [UsedImplicitly] init; } = string.Empty;
}
