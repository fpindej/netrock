namespace MyProject.Application.Features.Authentication.Dtos;

/// <summary>
/// Output from the external OAuth2 callback handler.
/// Contains tokens for authentication, plus metadata about the operation.
/// </summary>
/// <param name="Tokens">The authentication tokens, or null if this was a link-only operation.</param>
/// <param name="IsNewUser">Whether a new user account was created during this flow.</param>
/// <param name="Provider">The provider name that was used.</param>
/// <param name="IsLinkOnly">Whether this was an account-linking operation (user was already logged in).</param>
public record ExternalCallbackOutput(
    AuthenticationOutput? Tokens,
    bool IsNewUser,
    string Provider,
    bool IsLinkOnly);
