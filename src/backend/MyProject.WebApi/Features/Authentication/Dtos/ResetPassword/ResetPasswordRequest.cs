using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace MyProject.WebApi.Features.Authentication.Dtos.ResetPassword;

/// <summary>
/// Represents a request to reset a password using an opaque email token.
/// </summary>
[UsedImplicitly]
public class ResetPasswordRequest
{
    /// <summary>
    /// The opaque token received via the password reset email.
    /// </summary>
    [Required]
    public string Token { get; [UsedImplicitly] init; } = string.Empty;

    /// <summary>
    /// The new password to set.
    /// </summary>
    [Required]
    [MinLength(6)]
    [MaxLength(255)]
    public string NewPassword { get; [UsedImplicitly] init; } = string.Empty;
}
