using MyProject.Application.Features.Authentication.Dtos;
using MyProject.Shared;

namespace MyProject.Application.Features.Authentication;

/// <summary>
/// Provides external OAuth2 authentication operations including challenge initiation,
/// callback handling, provider discovery, and account linking/unlinking.
/// </summary>
public interface IExternalAuthService
{
    /// <summary>
    /// Creates an OAuth2 authorization challenge by generating a state token and building the provider's auth URL.
    /// </summary>
    /// <param name="input">The challenge input containing provider name and redirect URI.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result containing the authorization URL on success.</returns>
    Task<Result<ExternalChallengeOutput>> CreateChallengeAsync(ExternalChallengeInput input, CancellationToken cancellationToken);

    /// <summary>
    /// Handles the OAuth2 callback by validating state, exchanging the code, and performing account linking/creation.
    /// When the caller is authenticated, the provider is linked to the existing account.
    /// When unauthenticated, either logs in, auto-links by email, or creates a new account.
    /// </summary>
    /// <param name="input">The callback input containing the authorization code and state token.</param>
    /// <param name="useCookies">Whether to set authentication cookies.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result containing tokens and operation metadata on success.</returns>
    Task<Result<ExternalCallbackOutput>> HandleCallbackAsync(ExternalCallbackInput input, bool useCookies, CancellationToken cancellationToken);

    /// <summary>
    /// Unlinks an external provider from the current authenticated user's account.
    /// Fails if this is the user's last authentication method (no password and only one linked provider).
    /// </summary>
    /// <param name="provider">The provider name to unlink.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> UnlinkProviderAsync(string provider, CancellationToken cancellationToken);

    /// <summary>
    /// Returns all configured and enabled external authentication providers.
    /// </summary>
    /// <returns>A list of available provider information.</returns>
    IReadOnlyList<ExternalProviderInfo> GetAvailableProviders();

    /// <summary>
    /// Gets the names of external providers linked to a specific user.
    /// </summary>
    /// <param name="userId">The user ID to query.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of linked provider names.</returns>
    Task<IReadOnlyList<string>> GetLinkedProvidersAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Sets an initial password for a passwordless OAuth-created account.
    /// </summary>
    /// <param name="input">The input containing the new password.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> SetPasswordAsync(SetPasswordInput input, CancellationToken cancellationToken);
}
