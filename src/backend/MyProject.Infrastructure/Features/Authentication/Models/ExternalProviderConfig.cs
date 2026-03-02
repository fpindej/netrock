namespace MyProject.Infrastructure.Features.Authentication.Models;

/// <summary>
/// Persists per-provider OAuth2 configuration (credentials, enabled state)
/// so providers can be managed at runtime through the admin panel.
/// </summary>
internal class ExternalProviderConfig
{
    /// <summary>
    /// Gets or sets the primary key.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the unique provider identifier (e.g. "Google", "GitHub").
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this provider is currently enabled for login.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the AES-256-GCM encrypted client ID.
    /// </summary>
    public string EncryptedClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the AES-256-GCM encrypted client secret.
    /// </summary>
    public string EncryptedClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when this record was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets when this record was last updated (UTC).
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the user ID of the admin who last updated this record.
    /// </summary>
    public Guid? UpdatedBy { get; set; }
}
