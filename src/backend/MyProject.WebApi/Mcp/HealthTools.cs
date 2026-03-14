using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ModelContextProtocol.Server;

namespace MyProject.WebApi.Mcp;

/// <summary>
/// MCP tools for querying application health status.
/// </summary>
[McpServerToolType]
internal static class HealthTools
{
    /// <summary>
    /// Returns the structured health status of all registered health checks (database, S3, etc.).
    /// </summary>
    [McpServerTool(Name = "get-health"), Description("Get the health status of all application dependencies (database, S3, etc.)")]
    public static async Task<string> GetHealth(HealthCheckService healthCheckService, CancellationToken cancellationToken)
    {
        var report = await healthCheckService.CheckHealthAsync(cancellationToken);

        var result = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.ToString(),
                exception = e.Value.Exception?.Message
            }),
            totalDuration = report.TotalDuration.ToString()
        };

        return JsonSerializer.Serialize(result, JsonSerializerOptions.Web);
    }
}
