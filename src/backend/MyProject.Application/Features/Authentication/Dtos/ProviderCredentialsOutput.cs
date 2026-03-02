namespace MyProject.Application.Features.Authentication.Dtos;

/// <summary>
/// Decrypted provider credentials returned by <see cref="IProviderConfigService"/>.
/// </summary>
/// <param name="ClientId">The OAuth client ID.</param>
/// <param name="ClientSecret">The OAuth client secret.</param>
public record ProviderCredentialsOutput(string ClientId, string ClientSecret);
