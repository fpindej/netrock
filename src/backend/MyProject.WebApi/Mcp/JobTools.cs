using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using MyProject.Application.Features.Jobs;

namespace MyProject.WebApi.Mcp;

/// <summary>
/// MCP tools for querying and triggering background jobs.
/// </summary>
[McpServerToolType]
internal static class JobTools
{
    /// <summary>
    /// Lists all registered recurring jobs with their status, schedule, and last execution info.
    /// </summary>
    [McpServerTool(Name = "list-jobs"), Description("List all recurring background jobs with status and schedule.")]
    public static async Task<string> ListJobs(IJobManagementService jobManagementService)
    {
        var jobs = await jobManagementService.GetRecurringJobsAsync();

        return JsonSerializer.Serialize(jobs, JsonSerializerOptions.Web);
    }

    /// <summary>
    /// Triggers an immediate execution of a recurring job by its ID.
    /// </summary>
    [McpServerTool(Name = "trigger-job"), Description("Trigger immediate execution of a recurring job.")]
    public static async Task<string> TriggerJob(
        IJobManagementService jobManagementService,
        [Description("The recurring job identifier (e.g. 'expired-refresh-token-cleanup').")] string jobId)
    {
        var result = await jobManagementService.TriggerJobAsync(jobId);

        return result.IsSuccess
            ? JsonSerializer.Serialize(new { status = "triggered", jobId })
            : JsonSerializer.Serialize(new { error = result.Error });
    }
}
