namespace MyProject.Application.Features.Authentication.Dtos;

/// <summary>
/// Input for resetting a user's password using an opaque email token.
/// </summary>
/// <param name="Token">The opaque token received via email that maps to the Identity reset token and user.</param>
/// <param name="NewPassword">The new password to set.</param>
public record ResetPasswordInput(
    string Token,
    string NewPassword
);
