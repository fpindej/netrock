namespace MyProject.Application.Features.Authentication.Dtos;

/// <summary>
/// Input for resetting a user's password using a password reset token.
/// </summary>
/// <param name="Email">The email address of the account to reset.</param>
/// <param name="Token">The password reset token received via email.</param>
/// <param name="NewPassword">The new password to set.</param>
public record ResetPasswordInput(
    string Email,
    string Token,
    string NewPassword
);
