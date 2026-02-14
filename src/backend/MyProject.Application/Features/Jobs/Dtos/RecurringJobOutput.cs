namespace MyProject.Application.Features.Jobs.Dtos;

/// <summary>
/// Output representing a recurring job for list views.
/// </summary>
/// <param name="Id">The unique job identifier (e.g. "expired-refresh-token-cleanup").</param>
/// <param name="Cron">The cron expression for the job schedule.</param>
/// <param name="NextExecution">The next scheduled execution time, or null if paused.</param>
/// <param name="LastExecution">The last execution time, or null if never executed.</param>
/// <param name="LastStatus">The status of the most recent execution (e.g. "Succeeded", "Failed").</param>
/// <param name="IsPaused">Whether the job is currently paused.</param>
/// <param name="CreatedAt">When the job was registered.</param>
public record RecurringJobOutput(
    string Id,
    string Cron,
    DateTimeOffset? NextExecution,
    DateTimeOffset? LastExecution,
    string? LastStatus,
    bool IsPaused,
    DateTimeOffset? CreatedAt
);
