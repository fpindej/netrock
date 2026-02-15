namespace MyProject.Application.Features.Authentication.Dtos;

/// <summary>
/// Input for verifying a user's email address using a confirmation token.
/// </summary>
/// <param name="Email">The email address to verify.</param>
/// <param name="Token">The email confirmation token received via email.</param>
public record VerifyEmailInput(
    string Email,
    string Token
);
