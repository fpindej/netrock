namespace MyProject.Infrastructure.Features.Authentication.Models;

/// <summary>
/// Represents a pending two-factor authentication challenge issued after successful password validation.
/// The plaintext token is returned to the client; only the SHA-256 hash is stored.
/// </summary>
public class TwoFactorChallenge
{
    /// <summary>
    /// The unique identifier for this challenge.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The SHA-256 hash of the opaque challenge token sent to the client.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// The user who initiated the login and must complete the 2FA step.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// When this challenge was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When this challenge expires (UTC). Typically 5 minutes after creation.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Whether this challenge has already been consumed to complete a login.
    /// </summary>
    public bool IsUsed { get; set; }

    /// <summary>
    /// Whether the original login request included "remember me" — carried forward to token generation.
    /// </summary>
    public bool IsRememberMe { get; set; }

    /// <summary>
    /// Navigation property to the user.
    /// </summary>
    public ApplicationUser? User { get; set; }
}
