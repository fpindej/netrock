using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Aspire ServiceDefaults — wires up OpenTelemetry, service discovery, and HTTP client resilience.
/// Health-check endpoint mapping is intentionally omitted because the application already
/// provides <c>/health</c>, <c>/health/ready</c>, and <c>/health/live</c> via
/// <c>HealthCheckExtensions.MapHealthCheckEndpoints()</c>.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Adds Aspire service defaults: OpenTelemetry, service discovery, and HTTP resilience.
    /// Degrades gracefully when not running under Aspire (no OTEL endpoint configured).
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });

        return builder;
    }

    /// <summary>
    /// Configures OpenTelemetry logging, metrics, and tracing with OTLP export.
    /// All three signals are registered on the <see cref="OpenTelemetryBuilder"/> pipeline
    /// so that <see cref="OpenTelemetryBuilderExtensions.UseOtlpExporter"/> configures export for all of them.
    /// When <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> is not set (standalone run), the OTLP exporter is a no-op.
    /// </summary>
    private static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder)
    {
        builder.Services.AddOpenTelemetry()
            .WithLogging(
                configureBuilder: null,
                configureOptions: options =>
                {
                    options.IncludeFormattedMessage = true;
                    options.IncludeScopes = true;
                })
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation(options =>
                    {
                        // Filter out health check probe requests from traces
                        options.FilterHttpRequestMessage = httpRequestMessage =>
                            httpRequestMessage.RequestUri?.AbsolutePath.StartsWith("/health",
                                StringComparison.OrdinalIgnoreCase) != true;
                    })
                    .AddEntityFrameworkCoreInstrumentation();
            });

        AddOpenTelemetryExporters(builder);

        return builder;
    }

    /// <summary>
    /// Adds OTLP exporters when the endpoint environment variable is configured.
    /// </summary>
    private static void AddOpenTelemetryExporters(IHostApplicationBuilder builder)
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(
            builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }
    }

    /// <summary>
    /// Registers a basic liveness health check.
    /// Application-specific health checks (PostgreSQL, S3) are registered separately
    /// via <c>AddApplicationHealthChecks</c>.
    /// </summary>
    private static void AddDefaultHealthChecks(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);
    }
}
