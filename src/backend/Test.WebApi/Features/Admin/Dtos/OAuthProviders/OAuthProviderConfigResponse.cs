using JetBrains.Annotations;

namespace Test.WebApi.Features.Admin.Dtos.OAuthProviders;

/// <summary>
/// Represents an OAuth provider's configuration state for admin display.
/// </summary>
public class OAuthProviderConfigResponse
{
    /// <summary>
    /// The provider identifier (e.g. "Google", "GitHub").
    /// </summary>
    public string Provider { [UsedImplicitly] get; [UsedImplicitly] init; } = string.Empty;

    /// <summary>
    /// The human-readable display name.
    /// </summary>
    public string DisplayName { [UsedImplicitly] get; [UsedImplicitly] init; } = string.Empty;

    /// <summary>
    /// Whether the provider is currently enabled for login.
    /// </summary>
    public bool IsEnabled { [UsedImplicitly] get; [UsedImplicitly] init; }

    /// <summary>
    /// The OAuth client ID, or null if not configured.
    /// </summary>
    public string? ClientId { [UsedImplicitly] get; [UsedImplicitly] init; }

    /// <summary>
    /// Whether a client secret is stored.
    /// </summary>
    public bool HasClientSecret { [UsedImplicitly] get; [UsedImplicitly] init; }

    /// <summary>
    /// Where the configuration comes from: "database" or "unconfigured".
    /// </summary>
    public string Source { [UsedImplicitly] get; [UsedImplicitly] init; } = string.Empty;

    /// <summary>
    /// When the DB record was last updated, or null.
    /// </summary>
    public DateTime? UpdatedAt { [UsedImplicitly] get; [UsedImplicitly] init; }

    /// <summary>
    /// The admin user ID who last updated the record, or null.
    /// </summary>
    public Guid? UpdatedBy { [UsedImplicitly] get; [UsedImplicitly] init; }
}
