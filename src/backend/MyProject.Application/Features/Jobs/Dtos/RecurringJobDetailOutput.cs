namespace MyProject.Application.Features.Jobs.Dtos;

/// <summary>
/// Detailed output for a single recurring job, including recent execution history.
/// </summary>
/// <param name="Id">The unique job identifier.</param>
/// <param name="Cron">The cron expression for the job schedule.</param>
/// <param name="NextExecution">The next scheduled execution time, or null if paused.</param>
/// <param name="LastExecution">The last execution time, or null if never executed.</param>
/// <param name="LastStatus">The status of the most recent execution.</param>
/// <param name="IsPaused">Whether the job is currently paused.</param>
/// <param name="CreatedAt">When the job was registered.</param>
/// <param name="ExecutionHistory">Recent execution history entries.</param>
public record RecurringJobDetailOutput(
    string Id,
    string Cron,
    DateTimeOffset? NextExecution,
    DateTimeOffset? LastExecution,
    string? LastStatus,
    bool IsPaused,
    DateTimeOffset? CreatedAt,
    IReadOnlyList<JobExecutionOutput> ExecutionHistory
);
