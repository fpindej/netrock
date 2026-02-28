using System.ComponentModel.DataAnnotations;

namespace MyProject.Infrastructure.Caching.Options;

/// <summary>
/// Root caching configuration options.
/// When enabled, registers <see cref="Microsoft.Extensions.Caching.Hybrid.HybridCache"/>
/// with an L1 in-process memory cache. When disabled, a no-op implementation is used.
/// </summary>
public sealed class CachingOptions : IValidatableObject
{
    public const string SectionName = "Caching";

    /// <summary>
    /// Gets or sets a value indicating whether caching is enabled.
    /// When <c>false</c>, a no-op cache implementation is registered.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Gets or sets the default cache entry expiration.
    /// Used when no explicit expiration is provided.
    /// </summary>
    public TimeSpan DefaultExpiration { get; init; } = TimeSpan.FromMinutes(10);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DefaultExpiration <= TimeSpan.Zero)
        {
            yield return new ValidationResult(
                "DefaultExpiration must be greater than zero.",
                [nameof(DefaultExpiration)]);
        }
    }
}
