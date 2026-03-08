namespace Test.Application.Features.Authentication.Dtos;

/// <summary>
/// Output representing a provider's configuration state for admin display.
/// </summary>
/// <param name="Provider">The provider identifier (e.g. "Google").</param>
/// <param name="DisplayName">The human-readable display name.</param>
/// <param name="IsEnabled">Whether the provider is currently enabled.</param>
/// <param name="ClientId">The (unmasked) client ID, or null if not configured.</param>
/// <param name="HasClientSecret">Whether a client secret is stored (never expose the actual value).</param>
/// <param name="Source">Where the configuration comes from: "database" or "unconfigured".</param>
/// <param name="UpdatedAt">When the DB record was last updated, or null.</param>
/// <param name="UpdatedBy">The admin user ID who last updated the record, or null.</param>
public record ProviderConfigOutput(
    string Provider,
    string DisplayName,
    bool IsEnabled,
    string? ClientId,
    bool HasClientSecret,
    string Source,
    DateTime? UpdatedAt,
    Guid? UpdatedBy);
