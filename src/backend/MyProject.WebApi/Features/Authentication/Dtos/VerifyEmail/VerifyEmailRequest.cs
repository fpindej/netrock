using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace MyProject.WebApi.Features.Authentication.Dtos.VerifyEmail;

/// <summary>
/// Represents a request to verify an email address using an opaque email token.
/// </summary>
[UsedImplicitly]
public class VerifyEmailRequest
{
    /// <summary>
    /// The opaque token received via the email verification email.
    /// </summary>
    [Required]
    public string Token { get; [UsedImplicitly] init; } = string.Empty;
}
