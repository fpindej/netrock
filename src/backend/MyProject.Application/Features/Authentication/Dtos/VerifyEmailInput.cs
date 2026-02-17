namespace MyProject.Application.Features.Authentication.Dtos;

/// <summary>
/// Input for verifying a user's email address using an opaque email token.
/// </summary>
/// <param name="Token">The opaque token received via email that maps to the Identity confirmation token and user.</param>
public record VerifyEmailInput(
    string Token
);
