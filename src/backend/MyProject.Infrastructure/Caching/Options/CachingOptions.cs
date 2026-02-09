using System.ComponentModel.DataAnnotations;

namespace MyProject.Infrastructure.Caching.Options;

/// <summary>
/// Root caching configuration options.
/// Contains both Redis and InMemory cache configurations.
/// </summary>
public sealed class CachingOptions : IValidatableObject
{
    public const string SectionName = "Caching";

    /// <summary>
    /// Gets or sets the default cache entry expiration.
    /// Used when no explicit expiration is provided.
    /// Applies to both Redis and InMemory caching.
    /// </summary>
    public TimeSpan DefaultExpiration { get; init; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Gets or sets the Redis cache configuration.
    /// When Redis.Enabled is true, Redis will be used as the distributed cache.
    /// </summary>
    public RedisOptions Redis { get; init; } = new();

    /// <summary>
    /// Gets or sets the in-memory cache configuration.
    /// Used as fallback when Redis is disabled.
    /// </summary>
    public InMemoryOptions InMemory { get; init; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DefaultExpiration <= TimeSpan.Zero)
        {
            yield return new ValidationResult(
                "DefaultExpiration must be greater than zero.",
                [nameof(DefaultExpiration)]);
        }

        if (Redis.Enabled)
        {
            // Validate Redis options when enabled
            foreach (var result in Redis.Validate(validationContext))
            {
                yield return result;
            }
        }
        else
        {
            // Validate InMemory options when Redis is disabled
            foreach (var result in InMemory.Validate(validationContext))
            {
                yield return result;
            }
        }
    }

    /// <summary>
    /// Configuration options for Redis distributed cache.
    /// Validated only when Enabled is true.
    /// </summary>
    public sealed class RedisOptions : IValidatableObject
    {
        /// <summary>
        /// Gets or sets a value indicating whether Redis caching is enabled.
        /// When false, falls back to in-memory distributed cache.
        /// </summary>
        public bool Enabled { get; init; }

        /// <summary>
        /// Gets or sets the Redis connection string (host:port format).
        /// Required when Enabled is true.
        /// Example: "localhost:6379" or "redis-server.example.com:6380"
        /// </summary>
        public string ConnectionString { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the Redis authentication password.
        /// Required for production environments.
        /// </summary>
        public string? Password { get; init; }

        /// <summary>
        /// Gets or sets whether SSL/TLS should be used for the Redis connection.
        /// Should be true for production, especially cloud-hosted Redis.
        /// </summary>
        public bool UseSsl { get; init; }

        /// <summary>
        /// Gets or sets the default Redis database number (0-15).
        /// </summary>
        public int DefaultDatabase { get; init; }

        /// <summary>
        /// Gets or sets the instance name prefix for cache keys.
        /// Helps namespace keys when multiple apps share Redis.
        /// </summary>
        public string InstanceName { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the connection timeout in milliseconds.
        /// </summary>
        public int ConnectTimeoutMs { get; init; } = 5000;

        /// <summary>
        /// Gets or sets the synchronous operation timeout in milliseconds.
        /// </summary>
        public int SyncTimeoutMs { get; init; } = 5000;

        /// <summary>
        /// Gets or sets the asynchronous operation timeout in milliseconds.
        /// </summary>
        public int AsyncTimeoutMs { get; init; } = 5000;

        /// <summary>
        /// Gets or sets whether to abort connection if initial connect fails.
        /// Set to false for production to allow retry in background.
        /// </summary>
        public bool AbortOnConnectFail { get; init; } = true;

        /// <summary>
        /// Gets or sets the number of times to retry connection.
        /// </summary>
        public int ConnectRetry { get; init; } = 3;

        /// <summary>
        /// Gets or sets the keepalive interval in seconds.
        /// Sends periodic pings to keep connection alive.
        /// </summary>
        public int KeepAliveSeconds { get; init; } = 60;

        /// <summary>
        /// Validates Redis options. Only called when Redis is enabled.
        /// </summary>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                yield return new ValidationResult(
                    "ConnectionString is required when Redis is enabled.",
                    [nameof(ConnectionString)]);
            }

            if (DefaultDatabase is < 0 or > 15)
            {
                yield return new ValidationResult(
                    "DefaultDatabase must be between 0 and 15.",
                    [nameof(DefaultDatabase)]);
            }

            if (ConnectTimeoutMs <= 0)
            {
                yield return new ValidationResult(
                    "ConnectTimeoutMs must be greater than 0.",
                    [nameof(ConnectTimeoutMs)]);
            }

            if (SyncTimeoutMs <= 0)
            {
                yield return new ValidationResult(
                    "SyncTimeoutMs must be greater than 0.",
                    [nameof(SyncTimeoutMs)]);
            }

            if (AsyncTimeoutMs <= 0)
            {
                yield return new ValidationResult(
                    "AsyncTimeoutMs must be greater than 0.",
                    [nameof(AsyncTimeoutMs)]);
            }

            if (ConnectRetry < 0)
            {
                yield return new ValidationResult(
                    "ConnectRetry must be non-negative.",
                    [nameof(ConnectRetry)]);
            }

            if (KeepAliveSeconds <= 0)
            {
                yield return new ValidationResult(
                    "KeepAliveSeconds must be greater than 0.",
                    [nameof(KeepAliveSeconds)]);
            }
        }
    }

    /// <summary>
    /// Configuration options for in-memory distributed cache.
    /// Used when Redis is disabled.
    /// </summary>
    public sealed class InMemoryOptions : IValidatableObject
    {
        /// <summary>
        /// Gets or sets the size limit for in-memory cache entries.
        /// Each cached item should specify a size; this limits total size.
        /// Required when Redis is disabled. Set to null for no limit (not recommended for production).
        /// </summary>
        public int? SizeLimit { get; init; } = 1024;

        /// <summary>
        /// Gets or sets the minimum length of time between successive scans for expired items.
        /// </summary>
        public TimeSpan ExpirationScanFrequency { get; init; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Gets or sets the maximum percentage of the cache that can be compacted during a compaction operation.
        /// Value should be between 0 and 1 (e.g., 0.05 = 5%).
        /// </summary>
        public double CompactionPercentage { get; init; } = 0.05;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (SizeLimit is null)
            {
                yield return new ValidationResult(
                    "SizeLimit is required when Redis is disabled. Set a positive value or enable Redis.",
                    [nameof(SizeLimit)]);
            }
            else if (SizeLimit <= 0)
            {
                yield return new ValidationResult(
                    "SizeLimit must be greater than 0.",
                    [nameof(SizeLimit)]);
            }

            if (ExpirationScanFrequency <= TimeSpan.Zero)
            {
                yield return new ValidationResult(
                    "ExpirationScanFrequency must be greater than zero.",
                    [nameof(ExpirationScanFrequency)]);
            }

            if (CompactionPercentage is <= 0 or > 1)
            {
                yield return new ValidationResult(
                    "CompactionPercentage must be between 0 (exclusive) and 1 (inclusive).",
                    [nameof(CompactionPercentage)]);
            }
        }
    }
}
