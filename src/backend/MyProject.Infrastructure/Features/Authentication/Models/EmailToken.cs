namespace MyProject.Infrastructure.Features.Authentication.Models;

/// <summary>
/// Maps an opaque, URL-safe token to an ASP.NET Identity token and user,
/// so that password-reset and email-verification links never expose the user's email.
/// Tokens are SHA-256 hashed before storage.
/// </summary>
public class EmailToken
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the SHA-256 hash of the opaque token value sent in the URL.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the original ASP.NET Identity token (password-reset or email-confirmation).
    /// </summary>
    public string IdentityToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the purpose of this token.
    /// </summary>
    public EmailTokenPurpose Purpose { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the token was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the token expires.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets whether the token has been consumed.
    /// </summary>
    public bool IsUsed { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user this token was issued for.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the owning user.
    /// </summary>
    public ApplicationUser? User { get; set; }
}
