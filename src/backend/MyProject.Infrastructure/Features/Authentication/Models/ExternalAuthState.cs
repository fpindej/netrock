namespace MyProject.Infrastructure.Features.Authentication.Models;

/// <summary>
/// Represents a pending OAuth2 authorization state token.
/// The plaintext token is returned to the client; only the SHA-256 hash is stored.
/// Used for CSRF protection during the OAuth2 authorization code flow.
/// </summary>
public class ExternalAuthState
{
    /// <summary>
    /// Gets or sets the unique identifier for this state entry.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the SHA-256 hash of the opaque state token sent to the client.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the OAuth2 provider name (e.g. "Google", "GitHub").
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the redirect URI stored for the code exchange step.
    /// OAuth2 spec requires the exact redirect URI used in the authorization request.
    /// </summary>
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional user ID when this state was created by an authenticated user
    /// for account linking purposes.
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when this state was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when this state expires.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets whether this state token has already been consumed.
    /// </summary>
    public bool IsUsed { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the owning user (null for unauthenticated flows).
    /// </summary>
    public ApplicationUser? User { get; set; }
}
