using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyProject.Application.Caching;
using MyProject.Infrastructure.Caching.Options;

namespace MyProject.Infrastructure.Caching.Services;

/// <summary>
/// Distributed-cache-backed implementation of <see cref="ICacheService"/> using JSON serialization.
/// All <see cref="IDistributedCache"/> operations are wrapped in try/catch so that a cache
/// infrastructure outage (e.g. Redis down) degrades gracefully instead of crashing the request.
/// </summary>
internal class CacheService(
    IDistributedCache distributedCache,
    IOptions<CachingOptions> cachingOptions,
    ILogger<CacheService> logger) : ICacheService
{
    private readonly TimeSpan _defaultExpiration = cachingOptions.Value.DefaultExpiration;

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var cachedValue = await distributedCache.GetStringAsync(key, cancellationToken);
            return string.IsNullOrEmpty(cachedValue) ? default : JsonSerializer.Deserialize<T>(cachedValue);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache get failed for key '{CacheKey}', returning default", key);
            return default;
        }
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(
        string key,
        T value,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var serializedValue = JsonSerializer.Serialize(value);
            await distributedCache.SetStringAsync(key, serializedValue, ToDistributedOptions(options), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache set failed for key '{CacheKey}'", key);
        }
    }

    /// <inheritdoc />
    public async Task<T?> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var value = await factory(cancellationToken);
        if (value is not null)
        {
            await SetAsync(key, value, options, cancellationToken);
        }

        return value;
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await distributedCache.RemoveAsync(key, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache remove failed for key '{CacheKey}'", key);
        }
    }

    private DistributedCacheEntryOptions ToDistributedOptions(CacheEntryOptions? options)
    {
        if (options is null)
        {
            return new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _defaultExpiration
            };
        }

        return new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = options.AbsoluteExpiration,
            AbsoluteExpirationRelativeToNow = options.AbsoluteExpirationRelativeToNow,
            SlidingExpiration = options.SlidingExpiration
        };
    }
}
