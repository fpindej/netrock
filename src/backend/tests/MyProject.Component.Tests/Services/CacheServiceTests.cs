using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using MyProject.Infrastructure.Caching.Extensions;
using MyProject.Infrastructure.Caching.Options;
using MyProject.Infrastructure.Caching.Services;
using Polly;
using Polly.CircuitBreaker;
using Polly.Registry;

namespace MyProject.Component.Tests.Services;

public class CacheServiceTests
{
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<CacheService> _logger;
    private readonly CacheService _sut;

    public CacheServiceTests()
    {
        _distributedCache = Substitute.For<IDistributedCache>();
        _logger = Substitute.For<ILogger<CacheService>>();

        var options = Options.Create(new CachingOptions());
        var provider = CreateNoOpPipelineProvider();

        _sut = new CacheService(_distributedCache, options, provider, _logger);
    }

    #region GetAsync — Resilience

    [Fact]
    public async Task GetAsync_WhenCacheThrows_ReturnsDefault()
    {
        _distributedCache
            .GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<byte[]?>(_ => throw new InvalidOperationException("Redis down"));

        var result = await _sut.GetAsync<string>("key");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_WhenCacheThrows_LogsWarning()
    {
        _distributedCache
            .GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<byte[]?>(_ => throw new InvalidOperationException("Redis down"));

        await _sut.GetAsync<string>("key");

        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<InvalidOperationException>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    #endregion

    #region SetAsync — Resilience

    [Fact]
    public async Task SetAsync_WhenCacheThrows_DoesNotPropagate()
    {
        _distributedCache
            .SetAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException("Redis down"));

        var exception = await Record.ExceptionAsync(() => _sut.SetAsync("key", "value"));

        Assert.Null(exception);
    }

    [Fact]
    public async Task SetAsync_WhenCacheThrows_LogsWarning()
    {
        _distributedCache
            .SetAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException("Redis down"));

        await _sut.SetAsync("key", "value");

        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<InvalidOperationException>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    #endregion

    #region RemoveAsync — Resilience

    [Fact]
    public async Task RemoveAsync_WhenCacheThrows_DoesNotPropagate()
    {
        _distributedCache
            .RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException("Redis down"));

        var exception = await Record.ExceptionAsync(() => _sut.RemoveAsync("key"));

        Assert.Null(exception);
    }

    [Fact]
    public async Task RemoveAsync_WhenCacheThrows_LogsWarning()
    {
        _distributedCache
            .RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException("Redis down"));

        await _sut.RemoveAsync("key");

        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<InvalidOperationException>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    #endregion

    #region GetOrSetAsync — Happy Path

    [Fact]
    public async Task GetOrSetAsync_WhenCacheHit_ReturnsValueWithoutCallingFactory()
    {
        var json = System.Text.Json.JsonSerializer.Serialize("cached-value");
        _distributedCache
            .GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(System.Text.Encoding.UTF8.GetBytes(json));

        var factoryCalled = false;

        var result = await _sut.GetOrSetAsync(
            "key",
            _ =>
            {
                factoryCalled = true;
                return Task.FromResult("factory-value");
            });

        Assert.Equal("cached-value", result);
        Assert.False(factoryCalled);
    }

    [Fact]
    public async Task GetOrSetAsync_WhenCacheMiss_CallsFactoryAndReturnsResult()
    {
        _distributedCache
            .GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((byte[]?)null);

        var result = await _sut.GetOrSetAsync(
            "key",
            _ => Task.FromResult("factory-value"));

        Assert.Equal("factory-value", result);
    }

    [Fact]
    public async Task GetOrSetAsync_WhenCacheMiss_CachesFactoryResult()
    {
        _distributedCache
            .GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((byte[]?)null);

        await _sut.GetOrSetAsync(
            "key",
            _ => Task.FromResult("factory-value"));

        await _distributedCache.Received(1).SetAsync(
            "key",
            Arg.Any<byte[]>(),
            Arg.Any<DistributedCacheEntryOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetOrSetAsync_WhenFactoryThrows_PropagatesException()
    {
        _distributedCache
            .GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((byte[]?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.GetOrSetAsync<string>(
                "key",
                _ => throw new InvalidOperationException("DB failure")));
    }

    #endregion

    #region GetOrSetAsync — Resilience

    [Fact]
    public async Task GetOrSetAsync_WhenGetThrows_FallsThroughToFactory()
    {
        _distributedCache
            .GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<byte[]?>(_ => throw new InvalidOperationException("Redis down"));

        // SetAsync will also throw since Redis is down
        _distributedCache
            .SetAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException("Redis down"));

        var result = await _sut.GetOrSetAsync(
            "key",
            _ => Task.FromResult("factory-value"));

        Assert.Equal("factory-value", result);
    }

    [Fact]
    public async Task GetOrSetAsync_WhenSetThrows_StillReturnsFactoryValue()
    {
        // Get returns null (cache miss)
        _distributedCache
            .GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((byte[]?)null);

        // Set throws
        _distributedCache
            .SetAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException("Redis down"));

        var result = await _sut.GetOrSetAsync(
            "key",
            _ => Task.FromResult("factory-value"));

        Assert.Equal("factory-value", result);
    }

    #endregion

    #region Circuit Breaker

    [Fact]
    public async Task GetAsync_WhenCircuitOpen_ReturnsDefaultWithoutHittingCache()
    {
        var (sut, distributedCache, _) = CreateCircuitBreakerSut(failureThreshold: 2);

        distributedCache
            .GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<byte[]?>(_ => throw new InvalidOperationException("Redis down"));

        // Trip the circuit breaker (2 failures to meet threshold)
        await sut.GetAsync<string>("key1");
        await sut.GetAsync<string>("key2");

        // Clear received calls so we can assert the next call doesn't hit the cache
        distributedCache.ClearReceivedCalls();

        // Circuit is now open — this should return default without hitting IDistributedCache
        var result = await sut.GetAsync<string>("key3");

        Assert.Null(result);
        await distributedCache.DidNotReceive().GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetAsync_WhenCircuitOpen_SkipsWithoutHittingCache()
    {
        var (sut, distributedCache, _) = CreateCircuitBreakerSut(failureThreshold: 2);

        distributedCache
            .SetAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException("Redis down"));

        // Trip the circuit
        await sut.SetAsync("key1", "value");
        await sut.SetAsync("key2", "value");

        distributedCache.ClearReceivedCalls();

        // Circuit is now open
        var exception = await Record.ExceptionAsync(() => sut.SetAsync("key3", "value"));

        Assert.Null(exception);
        await distributedCache.DidNotReceive().SetAsync(
            Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveAsync_WhenCircuitOpen_SkipsWithoutHittingCache()
    {
        var (sut, distributedCache, _) = CreateCircuitBreakerSut(failureThreshold: 2);

        distributedCache
            .RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException("Redis down"));

        // Trip the circuit
        await sut.RemoveAsync("key1");
        await sut.RemoveAsync("key2");

        distributedCache.ClearReceivedCalls();

        // Circuit is now open
        var exception = await Record.ExceptionAsync(() => sut.RemoveAsync("key3"));

        Assert.Null(exception);
        await distributedCache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_WhenCircuitOpen_DoesNotLogWarning()
    {
        var (sut, distributedCache, logger) = CreateCircuitBreakerSut(failureThreshold: 2);

        distributedCache
            .GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<byte[]?>(_ => throw new InvalidOperationException("Redis down"));

        // Trip the circuit
        await sut.GetAsync<string>("key1");
        await sut.GetAsync<string>("key2");

        logger.ClearReceivedCalls();

        // Circuit is open — should not log per-operation warning
        await sut.GetAsync<string>("key3");

        logger.DidNotReceive().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task GetAsync_AfterBreakDurationElapsesAndProbeSucceeds_ResumesNormally()
    {
        var timeProvider = new FakeTimeProvider();
        var breakDuration = TimeSpan.FromSeconds(5);
        var (sut, distributedCache, _) = CreateCircuitBreakerSut(
            failureThreshold: 2,
            breakDuration: breakDuration,
            timeProvider: timeProvider);

        distributedCache
            .GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<byte[]?>(_ => throw new InvalidOperationException("Redis down"));

        // Trip the circuit
        await sut.GetAsync<string>("key1");
        await sut.GetAsync<string>("key2");

        // Advance time past break duration to allow half-open probe
        timeProvider.Advance(breakDuration + TimeSpan.FromMilliseconds(100));

        // Redis is back — return a real value for the probe
        var json = System.Text.Json.JsonSerializer.Serialize("recovered-value");
        distributedCache
            .GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(System.Text.Encoding.UTF8.GetBytes(json));

        // Half-open probe succeeds → circuit closes → normal operation
        var result = await sut.GetAsync<string>("probe-key");

        Assert.Equal("recovered-value", result);
    }

    #endregion

    #region Helpers

    private static ResiliencePipelineProvider<string> CreateNoOpPipelineProvider()
    {
        var services = new ServiceCollection();
        services.AddResiliencePipeline(ServiceCollectionExtensions.CachePipelineKey, static _ => { });
        var sp = services.BuildServiceProvider();
        return sp.GetRequiredService<ResiliencePipelineProvider<string>>();
    }

    private static (CacheService Sut, IDistributedCache Cache, ILogger<CacheService> Logger) CreateCircuitBreakerSut(
        int failureThreshold = 2,
        TimeSpan? breakDuration = null,
        TimeSpan? samplingDuration = null,
        FakeTimeProvider? timeProvider = null)
    {
        var actualBreakDuration = breakDuration ?? TimeSpan.FromSeconds(30);
        var actualSamplingDuration = samplingDuration ?? TimeSpan.FromSeconds(30);

        var distributedCache = Substitute.For<IDistributedCache>();
        var logger = Substitute.For<ILogger<CacheService>>();
        var options = Options.Create(new CachingOptions());

        var services = new ServiceCollection();

        if (timeProvider is not null)
        {
            services.AddSingleton<TimeProvider>(timeProvider);
        }

        services.AddResiliencePipeline(ServiceCollectionExtensions.CachePipelineKey, builder =>
        {
            builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 1.0,
                MinimumThroughput = failureThreshold,
                BreakDuration = actualBreakDuration,
                SamplingDuration = actualSamplingDuration,
                ShouldHandle = new PredicateBuilder().Handle<Exception>()
            });
        });

        var sp = services.BuildServiceProvider();
        var provider = sp.GetRequiredService<ResiliencePipelineProvider<string>>();

        var sut = new CacheService(distributedCache, options, provider, logger);
        return (sut, distributedCache, logger);
    }

    #endregion
}
