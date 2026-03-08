namespace Test.Application.Caching.Constants;

/// <summary>
/// Provides factory methods for generating standardized cache keys.
/// </summary>
public static class CacheKeys
{
    /// <summary>
    /// Returns the cache key for a user's profile data.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <returns>A cache key in the format <c>user:{userId}</c>.</returns>
    public static string User(Guid userId) => $"user:{userId}";

    /// <summary>
    /// Returns the cache key for a user's hashed security stamp.
    /// Used to validate JWT tokens against the current security stamp without hitting the database on every request.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <returns>A cache key in the format <c>security-stamp:{userId}</c>.</returns>
    public static string SecurityStamp(Guid userId) => $"security-stamp:{userId}";

    /// <summary>
    /// Returns the cache key for an OAuth provider's configuration and credentials.
    /// </summary>
    /// <param name="provider">The provider identifier (e.g. "Google").</param>
    /// <returns>A cache key in the format <c>provider-config:{provider}</c>.</returns>
    public static string ProviderConfig(string provider) => $"provider-config:{provider}";
}
