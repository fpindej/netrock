using JetBrains.Annotations;

namespace MyProject.WebApi.Features.Authentication.Dtos.External;

/// <summary>
/// Represents an available external authentication provider.
/// </summary>
public class ExternalProviderResponse
{
    /// <summary>
    /// The provider identifier (e.g. "Google", "GitHub").
    /// </summary>
    public string Name { [UsedImplicitly] get; [UsedImplicitly] init; } = string.Empty;

    /// <summary>
    /// The human-readable display name.
    /// </summary>
    public string DisplayName { [UsedImplicitly] get; [UsedImplicitly] init; } = string.Empty;
}
