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

public class CacheServiceTests : IDisposable
{
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<CacheService> _logger;
    private readonly CacheService _sut;
    private readonly ServiceProvider _serviceProvider;

    public CacheServiceTests()
    {
        _distributedCache = Substitute.For<IDistributedCache>();
        _logger = Substitute.For<ILogger<CacheService>>();

        var options = Options.Create(new CachingOptions());
        (_serviceProvider, var provider) = CreateNoOpPipelineProvider();

        _sut = new CacheService(_distributedCache, options, provider, _logger);
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
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
        using var fixture = CreateCircuitBreakerSut(failureThreshold: 2);

        fixture.Cache
            .GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<byte[]?>(_ => throw new InvalidOperationException("Redis down"));

        // Trip the circuit breaker (2 failures to meet threshold)
        await fixture.Sut.GetAsync<string>("key1");
        await fixture.Sut.GetAsync<string>("key2");

        // Clear received calls so we can assert the next call doesn't hit the cache
        fixture.Cache.ClearReceivedCalls();

        // Circuit is now open — this should return default without hitting IDistributedCache
        var result = await fixture.Sut.GetAsync<string>("key3");

        Assert.Null(result);
        await fixture.Cache.DidNotReceive().GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetAsync_WhenCircuitOpen_SkipsWithoutHittingCache()
    {
        using var fixture = CreateCircuitBreakerSut(failureThreshold: 2);

        fixture.Cache
            .SetAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException("Redis down"));

        // Trip the circuit
        await fixture.Sut.SetAsync("key1", "value");
        await fixture.Sut.SetAsync("key2", "value");

        fixture.Cache.ClearReceivedCalls();

        // Circuit is now open
        var exception = await Record.ExceptionAsync(() => fixture.Sut.SetAsync("key3", "value"));

        Assert.Null(exception);
        await fixture.Cache.DidNotReceive().SetAsync(
            Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveAsync_WhenCircuitOpen_SkipsWithoutHittingCache()
    {
        using var fixture = CreateCircuitBreakerSut(failureThreshold: 2);

        fixture.Cache
            .RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException("Redis down"));

        // Trip the circuit
        await fixture.Sut.RemoveAsync("key1");
        await fixture.Sut.RemoveAsync("key2");

        fixture.Cache.ClearReceivedCalls();

        // Circuit is now open
        var exception = await Record.ExceptionAsync(() => fixture.Sut.RemoveAsync("key3"));

        Assert.Null(exception);
        await fixture.Cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_WhenCircuitOpen_DoesNotLogWarning()
    {
        using var fixture = CreateCircuitBreakerSut(failureThreshold: 2);

        fixture.Cache
            .GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<byte[]?>(_ => throw new InvalidOperationException("Redis down"));

        // Trip the circuit
        await fixture.Sut.GetAsync<string>("key1");
        await fixture.Sut.GetAsync<string>("key2");

        fixture.Logger.ClearReceivedCalls();

        // Circuit is open — should not log per-operation warning
        await fixture.Sut.GetAsync<string>("key3");

        fixture.Logger.DidNotReceive().Log(
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
        using var fixture = CreateCircuitBreakerSut(
            failureThreshold: 2,
            breakDuration: breakDuration,
            timeProvider: timeProvider);

        fixture.Cache
            .GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<byte[]?>(_ => throw new InvalidOperationException("Redis down"));

        // Trip the circuit
        await fixture.Sut.GetAsync<string>("key1");
        await fixture.Sut.GetAsync<string>("key2");

        // Advance time past break duration to allow half-open probe
        timeProvider.Advance(breakDuration + TimeSpan.FromMilliseconds(100));

        // Redis is back — return a real value for the probe
        var json = System.Text.Json.JsonSerializer.Serialize("recovered-value");
        fixture.Cache
            .GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(System.Text.Encoding.UTF8.GetBytes(json));

        // Half-open probe succeeds → circuit closes → normal operation
        var result = await fixture.Sut.GetAsync<string>("probe-key");

        Assert.Equal("recovered-value", result);
    }

    [Fact]
    public async Task GetOrSetAsync_WhenCircuitOpen_FallsThroughToFactory()
    {
        using var fixture = CreateCircuitBreakerSut(failureThreshold: 2);

        // Both Get and Set will fail against Redis
        fixture.Cache
            .GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<byte[]?>(_ => throw new InvalidOperationException("Redis down"));
        fixture.Cache
            .SetAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException("Redis down"));

        // Trip the circuit via GetAsync failures
        await fixture.Sut.GetAsync<string>("trip1");
        await fixture.Sut.GetAsync<string>("trip2");

        // Circuit is open — GetOrSetAsync should fall through to factory
        var result = await fixture.Sut.GetOrSetAsync(
            "key",
            _ => Task.FromResult("factory-value"));

        Assert.Equal("factory-value", result);
    }

    #endregion

    #region Helpers

    private static (ServiceProvider ServiceProvider, ResiliencePipelineProvider<string> Provider) CreateNoOpPipelineProvider()
    {
        var services = new ServiceCollection();
        services.AddResiliencePipeline(ServiceCollectionExtensions.CachePipelineKey, static _ => { });
        var sp = services.BuildServiceProvider();
        return (sp, sp.GetRequiredService<ResiliencePipelineProvider<string>>());
    }

    private static CircuitBreakerFixture CreateCircuitBreakerSut(
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
        return new CircuitBreakerFixture(sut, distributedCache, logger, sp);
    }

    /// <summary>
    /// Encapsulates circuit breaker test dependencies and disposes the service provider.
    /// </summary>
    private sealed record CircuitBreakerFixture(
        CacheService Sut,
        IDistributedCache Cache,
        ILogger<CacheService> Logger,
        ServiceProvider ServiceProvider) : IDisposable
    {
        public void Dispose() => ServiceProvider.Dispose();
    }

    #endregion
}
