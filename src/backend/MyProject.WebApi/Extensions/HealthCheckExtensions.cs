using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MyProject.Infrastructure.Features.FileStorage.Options;

namespace MyProject.WebApi.Extensions;

/// <summary>
/// Extension methods for registering and mapping health check endpoints with dependency verification.
/// </summary>
internal static class HealthCheckExtensions
{
    private const string ReadyTag = "ready";

    /// <summary>
    /// Registers health checks for application dependencies (PostgreSQL and optionally S3).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration for reading connection strings.</param>
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
    ///   <item><c>/health/ready</c> — readiness checks only (DB), JSON response</item>
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
    /// Health check that verifies connectivity to S3-compatible storage.
    /// Creates the bucket on first successful connection if it does not exist.
    /// </summary>
    private sealed class S3HealthCheck(
        IAmazonS3 s3Client,
        IOptions<FileStorageOptions> options,
        ILogger<S3HealthCheck> logger) : IHealthCheck
    {
        private readonly SemaphoreSlim _bucketInitLock = new(1, 1);
        private bool _bucketEnsured;

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!_bucketEnsured)
                {
                    await EnsureBucketAsync(cancellationToken);
                }
                else
                {
                    await s3Client.GetBucketLocationAsync(
                        new GetBucketLocationRequest { BucketName = options.Value.BucketName },
                        cancellationToken);
                }

                return HealthCheckResult.Healthy();
            }
            catch (Exception ex)
            {
                return new HealthCheckResult(
                    context.Registration.FailureStatus,
                    "S3 storage is unreachable.",
                    ex);
            }
        }

        /// <summary>
        /// Creates the bucket if it does not already exist (idempotent, thread-safe).
        /// Uses double-check locking so only the first concurrent probe pays the S3 call cost.
        /// </summary>
        private async Task EnsureBucketAsync(CancellationToken ct)
        {
            if (_bucketEnsured) return;

            await _bucketInitLock.WaitAsync(ct);
            try
            {
                if (_bucketEnsured) return;

                await s3Client.PutBucketAsync(
                    new PutBucketRequest { BucketName = options.Value.BucketName }, ct);
                _bucketEnsured = true;
                logger.LogDebug("Bucket '{Bucket}' is ready", options.Value.BucketName);
            }
            catch (AmazonS3Exception ex) when (ex.ErrorCode is "BucketAlreadyOwnedByYou" or "BucketAlreadyExists")
            {
                _bucketEnsured = true;
                logger.LogDebug("Bucket '{Bucket}' already exists", options.Value.BucketName);
            }
            catch (AmazonS3Exception ex)
            {
                logger.LogWarning(ex, "Failed to ensure bucket '{Bucket}' exists", options.Value.BucketName);
                throw;
            }
            finally
            {
                _bucketInitLock.Release();
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
