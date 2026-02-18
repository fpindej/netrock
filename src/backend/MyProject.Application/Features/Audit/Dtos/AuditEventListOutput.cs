namespace MyProject.Application.Features.Audit.Dtos;

/// <summary>
/// Output for paginated audit event list results.
/// </summary>
/// <param name="Events">The list of audit events for the current page.</param>
/// <param name="TotalCount">The total number of events matching the query.</param>
/// <param name="PageNumber">The current page number.</param>
/// <param name="PageSize">The number of items per page.</param>
public record AuditEventListOutput(
    IReadOnlyList<AuditEventOutput> Events,
    int TotalCount,
    int PageNumber,
    int PageSize
);
