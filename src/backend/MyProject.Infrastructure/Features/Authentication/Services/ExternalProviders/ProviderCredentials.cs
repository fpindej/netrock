namespace MyProject.Infrastructure.Features.Authentication.Services.ExternalProviders;

/// <summary>
/// OAuth2 client credentials passed per-call to provider methods,
/// decoupling provider logic from configuration storage.
/// </summary>
internal sealed record ProviderCredentials(string ClientId, string ClientSecret);
