using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyProject.Application.Caching;
using MyProject.Infrastructure.Caching.Options;
using MyProject.Infrastructure.Caching.Services;

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

        _sut = new CacheService(_distributedCache, options, _logger);
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
}
