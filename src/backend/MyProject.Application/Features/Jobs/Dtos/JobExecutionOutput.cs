namespace MyProject.Application.Features.Jobs.Dtos;

/// <summary>
/// Output representing a single job execution history entry.
/// </summary>
/// <param name="JobId">The background job identifier for this execution.</param>
/// <param name="Status">The execution status (e.g. "Succeeded", "Failed", "Processing").</param>
/// <param name="StartedAt">When the execution started.</param>
/// <param name="Duration">How long the execution took, or null if still running.</param>
/// <param name="Error">The error message if the execution failed, or null on success.</param>
public record JobExecutionOutput(
    string JobId,
    string Status,
    DateTimeOffset? StartedAt,
    TimeSpan? Duration,
    string? Error
);
