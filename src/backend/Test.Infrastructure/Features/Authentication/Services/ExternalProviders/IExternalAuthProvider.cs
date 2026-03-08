using Test.Shared;

namespace Test.Infrastructure.Features.Authentication.Services.ExternalProviders;

/// <summary>
/// Abstraction for an external OAuth2/OIDC authentication provider.
/// Each provider knows how to build authorization URLs and exchange authorization codes for user info.
/// </summary>
internal interface IExternalAuthProvider
{
    /// <summary>
    /// Gets the unique identifier for this provider (e.g. "Google", "GitHub").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the human-readable display name shown in the UI.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Builds the full authorization URL that the client should redirect the user to.
    /// </summary>
    /// <param name="credentials">The OAuth2 client credentials for this provider.</param>
    /// <param name="state">The opaque state token for CSRF protection.</param>
    /// <param name="redirectUri">The callback URI the provider should redirect back to.</param>
    /// <param name="nonce">Optional nonce for OIDC providers.</param>
    /// <returns>The provider's authorization URL with all required query parameters.</returns>
    string BuildAuthorizationUrl(ProviderCredentials credentials, string state, string redirectUri, string? nonce = null);

    /// <summary>
    /// Exchanges an authorization code for user information from the provider.
    /// </summary>
    /// <param name="credentials">The OAuth2 client credentials for this provider.</param>
    /// <param name="code">The authorization code received from the provider callback.</param>
    /// <param name="redirectUri">The same redirect URI used in the authorization request (OAuth2 spec requires exact match).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user's external identity information.</returns>
    Task<ExternalUserInfo> ExchangeCodeAsync(ProviderCredentials credentials, string code, string redirectUri, CancellationToken cancellationToken);

    /// <summary>
    /// Tests whether the provider's OAuth credentials are valid by sending a dummy token request.
    /// A valid credential set returns <see cref="Result.Success()"/> (provider rejects the code but accepts the client),
    /// while invalid credentials return a failure result.
    /// </summary>
    /// <param name="credentials">The OAuth2 client credentials to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success if credentials are accepted by the provider, failure otherwise.</returns>
    Task<Result> TestConnectionAsync(ProviderCredentials credentials, CancellationToken cancellationToken);
}
