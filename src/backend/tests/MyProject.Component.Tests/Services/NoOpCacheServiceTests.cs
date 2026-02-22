using MyProject.Application.Caching;
using MyProject.Infrastructure.Caching.Services;

namespace MyProject.Component.Tests.Services;

public class NoOpCacheServiceTests
{
    private readonly NoOpCacheService _sut = new();

    [Fact]
    public async Task GetAsync_ReturnsDefault()
    {
        var result = await _sut.GetAsync<string>("any-key");

        Assert.Null(result);
    }

    [Fact]
    public async Task SetAsync_DoesNotThrow()
    {
        var exception = await Record.ExceptionAsync(
            () => _sut.SetAsync("key", "value", CacheEntryOptions.AbsoluteExpireIn(TimeSpan.FromMinutes(5))));

        Assert.Null(exception);
    }

    [Fact]
    public async Task RemoveAsync_DoesNotThrow()
    {
        var exception = await Record.ExceptionAsync(
            () => _sut.RemoveAsync("key"));

        Assert.Null(exception);
    }

    [Fact]
    public async Task GetOrSetAsync_CallsFactory()
    {
        var factoryCalled = false;

        var result = await _sut.GetOrSetAsync(
            "key",
            _ =>
            {
                factoryCalled = true;
                return Task.FromResult("factory-value");
            });

        Assert.True(factoryCalled);
        Assert.Equal("factory-value", result);
    }

    [Fact]
    public async Task GetOrSetAsync_DoesNotCacheFactoryResult()
    {
        await _sut.GetOrSetAsync("key", _ => Task.FromResult("value"));

        // Second call should still return default from GetAsync (not the previously "set" value)
        var result = await _sut.GetAsync<string>("key");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetOrSetAsync_WhenFactoryThrows_PropagatesException()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.GetOrSetAsync<string>(
                "key",
                _ => throw new InvalidOperationException("DB failure")));

        Assert.Equal("DB failure", exception.Message);
    }
}
