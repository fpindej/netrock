using JetBrains.Annotations;

namespace Test.WebApi.Features.Admin.Dtos.OAuthProviders;

/// <summary>
/// Request body for creating or updating an OAuth provider's configuration.
/// </summary>
public class UpdateOAuthProviderRequest
{
    /// <summary>
    /// Whether to enable this provider for login.
    /// </summary>
    public bool IsEnabled { [UsedImplicitly] get; [UsedImplicitly] init; }

    /// <summary>
    /// The OAuth client ID.
    /// </summary>
    public string ClientId { [UsedImplicitly] get; [UsedImplicitly] init; } = string.Empty;

    /// <summary>
    /// The OAuth client secret, or null to keep the existing value.
    /// </summary>
    public string? ClientSecret { [UsedImplicitly] get; [UsedImplicitly] init; }
}
