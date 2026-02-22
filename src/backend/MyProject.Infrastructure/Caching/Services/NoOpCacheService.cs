using MyProject.Application.Caching;

namespace MyProject.Infrastructure.Caching.Services;

/// <summary>
/// No-op implementation of <see cref="ICacheService"/> used when caching is disabled.
/// All reads return <c>default</c>, all writes are ignored, and <see cref="GetOrSetAsync{T}"/>
/// delegates directly to the factory.
/// </summary>
internal class NoOpCacheService : ICacheService
{
    /// <inheritdoc />
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(default(T));
    }

    /// <inheritdoc />
    public Task SetAsync<T>(
        string key,
        T value,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<T?> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return await factory(cancellationToken);
    }

    /// <inheritdoc />
    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
