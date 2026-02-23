using Amazon.S3;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MyProject.Infrastructure.Caching.Options;
using MyProject.Infrastructure.Features.FileStorage.Options;

namespace MyProject.WebApi.Extensions;

/// <summary>
/// Extension methods for registering and mapping health check endpoints with dependency verification.
/// </summary>
internal static class HealthCheckExtensions
{
    private const string ReadyTag = "ready";

    /// <summary>
    /// Registers health checks for application dependencies (PostgreSQL and optionally Redis).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration for reading connection strings and caching options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddApplicationHealthChecks(this IServiceCollection services,
        IConfiguration configuration)
    {
        var healthChecks = services.AddHealthChecks();

        var connectionString = configuration.GetConnectionString("Database")
                               ?? throw new InvalidOperationException(
                                   "ConnectionStrings:Database is required for the health check.");

        healthChecks.AddNpgSql(
            connectionString,
            name: "PostgreSQL",
            timeout: TimeSpan.FromSeconds(3),
            tags: [ReadyTag]);

        var cachingOptions = configuration.GetSection(CachingOptions.SectionName).Get<CachingOptions>();

        if (cachingOptions?.Redis is { Enabled: true } redisOptions)
        {
            var redisConnectionString = BuildRedisConnectionString(redisOptions);

            healthChecks.AddRedis(
                redisConnectionString,
                name: "Redis",
                timeout: TimeSpan.FromSeconds(3),
                tags: [ReadyTag]);
        }

        var fileStorageOptions = configuration
            .GetSection(FileStorageOptions.SectionName)
            .Get<FileStorageOptions>();

        if (fileStorageOptions is not null && !string.IsNullOrEmpty(fileStorageOptions.Endpoint))
        {
            healthChecks.AddCheck<S3HealthCheck>(
                "S3",
                failureStatus: HealthStatus.Degraded,
                timeout: TimeSpan.FromSeconds(5),
                tags: [ReadyTag]);
        }

        return services;
    }

    /// <summary>
    /// Maps health check endpoints with rate limiting disabled:
    /// <list type="bullet">
    ///   <item><c>/health</c> — all checks, JSON response</item>
    ///   <item><c>/health/ready</c> — readiness checks only (DB + Redis), JSON response</item>
    ///   <item><c>/health/live</c> — no checks, always 200 Healthy, plain text</item>
    /// </list>
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The web application for chaining.</returns>
    public static WebApplication MapHealthCheckEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health", new()
            {
                ResponseWriter = WriteJsonResponse
            })
            .DisableRateLimiting();

        app.MapHealthChecks("/health/ready", new()
            {
                Predicate = check => check.Tags.Contains(ReadyTag),
                ResponseWriter = WriteJsonResponse
            })
            .DisableRateLimiting();

        app.MapHealthChecks("/health/live", new()
            {
                Predicate = _ => false
            })
            .DisableRateLimiting();

        return app;
    }

    /// <summary>
    /// Builds a StackExchange.Redis-compatible connection string from <see cref="CachingOptions.RedisOptions"/>.
    /// </summary>
    private static string BuildRedisConnectionString(CachingOptions.RedisOptions redisOptions)
    {
        var parts = new List<string> { redisOptions.ConnectionString };

        if (!string.IsNullOrWhiteSpace(redisOptions.Password))
        {
            parts.Add($"password={redisOptions.Password}");
        }

        if (redisOptions.UseSsl)
        {
            parts.Add("ssl=true");
        }

        return string.Join(",", parts);
    }

    /// <summary>
    /// Health check that verifies connectivity to S3-compatible storage.
    /// Uses HeadBucket which only requires <c>s3:HeadBucket</c> permission on the configured bucket,
    /// unlike ListBuckets which requires the global <c>s3:ListAllMyBuckets</c> permission.
    /// </summary>
    private sealed class S3HealthCheck(IAmazonS3 s3Client, IOptions<FileStorageOptions> options) : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                await s3Client.EnsureBucketExistsAsync(options.Value.BucketName);
                return HealthCheckResult.Healthy();
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("S3 storage is unreachable.", ex);
            }
        }
    }

    /// <summary>
    /// Writes a JSON health check response with per-check details.
    /// </summary>
    private static async Task WriteJsonResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                duration = entry.Value.Duration.ToString()
            }),
            totalDuration = report.TotalDuration.ToString()
        };

        await context.Response.WriteAsJsonAsync(response);
    }
}
