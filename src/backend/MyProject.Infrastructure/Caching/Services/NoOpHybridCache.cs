using Microsoft.Extensions.Caching.Hybrid;

namespace MyProject.Infrastructure.Caching.Services;

/// <summary>
/// No-op implementation of <see cref="HybridCache"/> used when caching is disabled.
/// <see cref="GetOrCreateAsync{TState,T}"/> delegates directly to the factory;
/// all other operations are silent no-ops.
/// </summary>
internal class NoOpHybridCache : HybridCache
{
    /// <inheritdoc />
    public override async ValueTask<T> GetOrCreateAsync<TState, T>(
        string key,
        TState state,
        Func<TState, CancellationToken, ValueTask<T>> factory,
        HybridCacheEntryOptions? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        return await factory(state, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask SetAsync<T>(
        string key,
        T value,
        HybridCacheEntryOptions? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override ValueTask RemoveAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override ValueTask RemoveByTagAsync(
        string tag,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }
}
