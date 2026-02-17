namespace MyProject.Infrastructure.Features.Authentication.Models;

/// <summary>
/// Identifies the purpose of an email token.
/// </summary>
public enum EmailTokenPurpose
{
    /// <summary>
    /// Token used for resetting a user's password.
    /// </summary>
    PasswordReset,

    /// <summary>
    /// Token used for confirming a user's email address.
    /// </summary>
    EmailVerification
}
