using MyProject.Application.Features.Authentication.Dtos;

namespace MyProject.Application.Features.Authentication;

/// <summary>
/// Manages OAuth provider configuration with DB storage and appsettings fallback.
/// </summary>
public interface IProviderConfigService
{
    /// <summary>
    /// Returns configuration state for all known providers.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of provider configurations.</returns>
    Task<IReadOnlyList<ProviderConfigOutput>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Returns decrypted credentials for the given provider, or null if not configured or disabled.
    /// Falls back to appsettings when no DB record exists.
    /// </summary>
    /// <param name="provider">The provider name (e.g. "Google").</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The provider info and credentials, or null.</returns>
    Task<ProviderCredentialsOutput?> GetCredentialsAsync(string provider, CancellationToken cancellationToken);

    /// <summary>
    /// Creates or updates a provider's configuration in the database.
    /// Encrypts secrets before storing, invalidates cache, and logs an audit event.
    /// </summary>
    /// <param name="callerUserId">The ID of the admin performing the update.</param>
    /// <param name="input">The provider configuration input.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task UpsertAsync(Guid callerUserId, UpsertProviderConfigInput input, CancellationToken cancellationToken);
}
