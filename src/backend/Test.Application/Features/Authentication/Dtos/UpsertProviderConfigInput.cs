namespace Test.Application.Features.Authentication.Dtos;

/// <summary>
/// Input for creating or updating a provider's OAuth configuration.
/// </summary>
/// <param name="Provider">The provider identifier (e.g. "Google").</param>
/// <param name="IsEnabled">Whether to enable this provider.</param>
/// <param name="ClientId">The OAuth client ID.</param>
/// <param name="ClientSecret">The OAuth client secret, or null to keep the existing value.</param>
public record UpsertProviderConfigInput(
    string Provider,
    bool IsEnabled,
    string ClientId,
    string? ClientSecret);
