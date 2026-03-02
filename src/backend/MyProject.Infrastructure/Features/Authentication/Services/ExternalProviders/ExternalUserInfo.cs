namespace MyProject.Infrastructure.Features.Authentication.Services.ExternalProviders;

/// <summary>
/// User information extracted from an external OAuth2 provider after code exchange.
/// </summary>
internal sealed record ExternalUserInfo(
    string ProviderKey,
    string Email,
    bool EmailVerified,
    string? FirstName,
    string? LastName);
