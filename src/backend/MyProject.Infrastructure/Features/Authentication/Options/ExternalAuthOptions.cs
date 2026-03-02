using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace MyProject.Infrastructure.Features.Authentication.Options;

/// <summary>
/// Configuration for external OAuth2/OIDC login providers.
/// Maps to the "Authentication:ExternalProviders" section in appsettings.json.
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
    /// Gets or sets the Google OAuth2/OIDC provider configuration.
    /// </summary>
    [ValidateObjectMembers]
    public ProviderOptions Google { get; init; } = new();

    /// <summary>
    /// Gets or sets the GitHub OAuth2 provider configuration.
    /// </summary>
    [ValidateObjectMembers]
    public ProviderOptions GitHub { get; init; } = new();

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StateLifetime < TimeSpan.FromMinutes(1) || StateLifetime > TimeSpan.FromMinutes(30))
        {
            yield return new ValidationResult(
                $"StateLifetime must be between 1 and 30 minutes, but was {StateLifetime}.",
                [nameof(StateLifetime)]);
        }

        foreach (var uri in AllowedRedirectUris)
        {
            if (!Uri.TryCreate(uri, UriKind.Absolute, out _))
            {
                yield return new ValidationResult(
                    $"AllowedRedirectUris contains an invalid URI: '{uri}'.",
                    [nameof(AllowedRedirectUris)]);
            }
        }
    }

    /// <summary>
    /// Configuration for a single external authentication provider.
    /// When <see cref="Enabled"/> is <c>true</c>, <see cref="ClientId"/> and <see cref="ClientSecret"/> are required.
    /// </summary>
    public sealed class ProviderOptions : IValidatableObject
    {
        /// <summary>
        /// Gets or sets whether this provider is enabled.
        /// When <c>false</c>, the provider is not registered and no UI elements are shown.
        /// </summary>
        public bool Enabled { get; [UsedImplicitly] init; }

        /// <summary>
        /// Gets or sets the OAuth2 client ID issued by the provider.
        /// </summary>
        public string ClientId { get; [UsedImplicitly] init; } = string.Empty;

        /// <summary>
        /// Gets or sets the OAuth2 client secret issued by the provider.
        /// </summary>
        public string ClientSecret { get; [UsedImplicitly] init; } = string.Empty;

        /// <inheritdoc />
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!Enabled)
            {
                yield break;
            }

            if (string.IsNullOrWhiteSpace(ClientId))
            {
                yield return new ValidationResult(
                    "ClientId is required when the provider is enabled.",
                    [nameof(ClientId)]);
            }

            if (string.IsNullOrWhiteSpace(ClientSecret))
            {
                yield return new ValidationResult(
                    "ClientSecret is required when the provider is enabled.",
                    [nameof(ClientSecret)]);
            }
        }
    }
}
