using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace Test.Infrastructure.Features.Authentication.Options;

/// <summary>
/// Configuration for external OAuth2/OIDC login providers.
/// Maps to the "Authentication:ExternalProviders" section in appsettings.json.
/// Provider credentials are managed exclusively via the database (admin API).
/// </summary>
public sealed class ExternalAuthOptions : IValidatableObject
{
    public const string SectionName = "Authentication:ExternalProviders";

    /// <summary>
    /// Gets or sets the whitelist of redirect URIs that clients may use as OAuth callback targets.
    /// Web apps use <c>https://</c> URLs; mobile apps use custom URL schemes.
    /// </summary>
    [Required]
    [MinLength(1)]
    public List<string> AllowedRedirectUris { get; init; } = [];

    /// <summary>
    /// Gets or sets the lifetime of OAuth state tokens.
    /// Defaults to 10 minutes. Valid range: 1-30 minutes.
    /// </summary>
    public TimeSpan StateLifetime { get; [UsedImplicitly] init; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Gets or sets the master encryption key used to protect OAuth client secrets at rest.
    /// Must be at least 32 characters. The raw value is fed through HKDF-SHA256 to derive the AES-256 key.
    /// </summary>
    [Required]
    [MinLength(32)]
    public string EncryptionKey { get; [UsedImplicitly] init; } = string.Empty;

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StateLifetime < TimeSpan.FromMinutes(1) || StateLifetime > TimeSpan.FromMinutes(30))
        {
            yield return new ValidationResult(
                $"StateLifetime must be between 1 and 30 minutes, but was {StateLifetime}.",
                [nameof(StateLifetime)]);
        }

        // Blocklist rather than allowlist so custom mobile schemes (e.g. myapp://oauth/callback) are permitted.
        foreach (var uri in AllowedRedirectUris)
        {
            if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsed))
            {
                yield return new ValidationResult(
                    $"AllowedRedirectUris contains an invalid URI: '{uri}'.",
                    [nameof(AllowedRedirectUris)]);
            }
            else if (parsed.Scheme is "javascript" or "data" or "file" or "blob" or "vbscript")
            {
                yield return new ValidationResult(
                    $"AllowedRedirectUris contains a URI with a dangerous scheme '{parsed.Scheme}': '{uri}'.",
                    [nameof(AllowedRedirectUris)]);
            }
        }
    }
}
