using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyProject.Application.Caching;
using MyProject.Infrastructure.Caching.Options;
using MyProject.Infrastructure.Caching.Services;
using StackExchange.Redis;

namespace MyProject.Infrastructure.Caching.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCaching(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<CachingOptions>()
            .BindConfiguration(CachingOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var cachingOptions = configuration.GetSection(CachingOptions.SectionName).Get<CachingOptions>();

        if (cachingOptions?.Redis.Enabled is true)
        {
            var configurationOptions = BuildConfigurationOptions(cachingOptions.Redis);

            services.AddStackExchangeRedisCache(options =>
            {
                options.ConfigurationOptions = configurationOptions;
                options.InstanceName = cachingOptions.Redis.InstanceName;
            });
        }
        else
        {
            var inMemoryOptions = cachingOptions?.InMemory ?? new InMemoryOptions();
            services.AddDistributedMemoryCache(options =>
            {
                options.SizeLimit = inMemoryOptions.SizeLimit;
                options.ExpirationScanFrequency = inMemoryOptions.ExpirationScanFrequency;
                options.CompactionPercentage = inMemoryOptions.CompactionPercentage;
            });
        }

        services.AddScoped<ICacheService, CacheService>();
        return services;
    }

    private static ConfigurationOptions BuildConfigurationOptions(RedisOptions redisOptions)
    {
        var configurationOptions = new ConfigurationOptions
        {
            DefaultDatabase = redisOptions.DefaultDatabase,
            Ssl = redisOptions.UseSsl,
            AbortOnConnectFail = redisOptions.AbortOnConnectFail,
            ConnectTimeout = redisOptions.ConnectTimeoutMs,
            SyncTimeout = redisOptions.SyncTimeoutMs,
            AsyncTimeout = redisOptions.AsyncTimeoutMs,
            ConnectRetry = redisOptions.ConnectRetry,
            KeepAlive = redisOptions.KeepAliveSeconds
        };

        // Parse connection string (host:port or host:port,host:port for cluster)
        foreach (var endpoint in redisOptions.ConnectionString.Split(','))
        {
            var trimmed = endpoint.Trim();
            if (!string.IsNullOrEmpty(trimmed))
            {
                configurationOptions.EndPoints.Add(trimmed);
            }
        }

        // Set password if provided
        if (!string.IsNullOrWhiteSpace(redisOptions.Password))
        {
            configurationOptions.Password = redisOptions.Password;
        }

        return configurationOptions;
    }
}
