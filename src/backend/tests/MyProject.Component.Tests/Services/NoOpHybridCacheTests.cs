using Microsoft.Extensions.Caching.Hybrid;
using MyProject.Infrastructure.Caching.Services;

namespace MyProject.Component.Tests.Services;

public class NoOpHybridCacheTests
{
    private readonly NoOpHybridCache _sut = new();

    [Fact]
    public async Task GetOrCreateAsync_DelegatesToFactory()
    {
        var result = await _sut.GetOrCreateAsync("key", async _ => "value");

        Assert.Equal("value", result);
    }

    [Fact]
    public async Task GetOrCreateAsync_ReturnsNull_WhenFactoryReturnsNull()
    {
        var result = await _sut.GetOrCreateAsync<string?>("key", async _ => null);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetOrCreateAsync_PropagatesFactoryException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _sut.GetOrCreateAsync<string>("key",
                async _ => throw new InvalidOperationException("factory error")));
    }

    [Fact]
    public async Task SetAsync_DoesNotThrow()
    {
        await _sut.SetAsync("key", "value");
    }

    [Fact]
    public async Task RemoveAsync_DoesNotThrow()
    {
        await _sut.RemoveAsync("key");
    }

    [Fact]
    public async Task RemoveByTagAsync_DoesNotThrow()
    {
        await _sut.RemoveByTagAsync("tag");
    }

    [Fact]
    public async Task GetOrCreateAsync_WithOptions_DelegatesToFactory()
    {
        var options = new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(5) };

        var result = await _sut.GetOrCreateAsync("key", async _ => 42, options);

        Assert.Equal(42, result);
    }
}
