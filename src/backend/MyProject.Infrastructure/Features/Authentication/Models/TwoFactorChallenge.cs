namespace MyProject.Infrastructure.Features.Authentication.Models;

/// <summary>
/// Represents a pending two-factor authentication challenge issued after successful password validation.
/// The plaintext token is returned to the client; only the SHA-256 hash is stored.
/// </summary>
public class TwoFactorChallenge
{
    /// <summary>
    /// Gets or sets the unique identifier for this challenge.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the SHA-256 hash of the opaque challenge token sent to the client.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ID of the user who initiated the login and must complete the 2FA step.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when this challenge was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when this challenge expires.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets whether this challenge has already been consumed to complete a login.
    /// </summary>
    public bool IsUsed { get; set; }

    /// <summary>
    /// Gets or sets whether the original login request included "remember me",
    /// carried forward to token generation on successful 2FA verification.
    /// </summary>
    public bool IsRememberMe { get; set; }

    /// <summary>
    /// Gets or sets the number of failed verification attempts against this challenge.
    /// When this exceeds the configured maximum, the challenge is locked.
    /// </summary>
    public int FailedAttempts { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the owning user.
    /// </summary>
    public ApplicationUser? User { get; set; }
}
