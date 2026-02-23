using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyProject.Application.Caching;
using MyProject.Infrastructure.Caching.Extensions;
using MyProject.Infrastructure.Caching.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Registry;

namespace MyProject.Infrastructure.Caching.Services;

/// <summary>
/// Distributed-cache-backed implementation of <see cref="ICacheService"/> using JSON serialization.
/// All <see cref="IDistributedCache"/> operations are wrapped in a Polly resilience pipeline so that
/// sustained cache outages are short-circuited — the circuit breaker eliminates per-operation latency
/// and log spam after the failure threshold is reached.
/// </summary>
internal class CacheService(
    IDistributedCache distributedCache,
    IOptions<CachingOptions> cachingOptions,
    ResiliencePipelineProvider<string> pipelineProvider,
    ILogger<CacheService> logger) : ICacheService
{
    private readonly TimeSpan _defaultExpiration = cachingOptions.Value.DefaultExpiration;
    private readonly ResiliencePipeline _pipeline = pipelineProvider.GetPipeline(ServiceCollectionExtensions.CachePipelineKey);

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var cachedValue = await _pipeline.ExecuteAsync(
                async ct => await distributedCache.GetStringAsync(key, ct),
                cancellationToken);

            return string.IsNullOrEmpty(cachedValue) ? default : JsonSerializer.Deserialize<T>(cachedValue);
        }
        catch (BrokenCircuitException)
        {
            return default;
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

            await _pipeline.ExecuteAsync(
                async ct => await distributedCache.SetStringAsync(key, serializedValue, ToDistributedOptions(options), ct),
                cancellationToken);
        }
        catch (BrokenCircuitException)
        {
            // Circuit is open — skip silently
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
            await _pipeline.ExecuteAsync(
                async ct => await distributedCache.RemoveAsync(key, ct),
                cancellationToken);
        }
        catch (BrokenCircuitException)
        {
            // Circuit is open — skip silently
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
