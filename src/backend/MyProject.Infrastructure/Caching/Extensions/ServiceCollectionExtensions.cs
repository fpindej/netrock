using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyProject.Infrastructure.Caching.Options;
using MyProject.Infrastructure.Caching.Services;

namespace MyProject.Infrastructure.Caching.Extensions;

/// <summary>
/// Extension methods for registering <see cref="HybridCache"/> caching services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="HybridCache"/> with an L1 in-process memory cache,
    /// or a no-op implementation when caching is disabled.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration for reading caching options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCaching(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<CachingOptions>()
            .BindConfiguration(CachingOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var cachingOptions = configuration.GetSection(CachingOptions.SectionName).Get<CachingOptions>();

        if (cachingOptions?.Enabled is false)
        {
            services.AddSingleton<HybridCache, NoOpHybridCache>();
            return services;
        }

        var defaultExpiration = cachingOptions?.DefaultExpiration ?? TimeSpan.FromMinutes(10);

        services.AddHybridCache(options =>
        {
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = defaultExpiration
            };
        });

        return services;
    }
}
