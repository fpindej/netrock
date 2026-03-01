namespace MyProject.Application.Features.Authentication.Dtos;

/// <summary>
/// Describes an available external authentication provider.
/// </summary>
/// <param name="Name">The provider identifier (e.g. "Google", "GitHub").</param>
/// <param name="DisplayName">The human-readable display name.</param>
public record ExternalProviderInfo(string Name, string DisplayName);
