using MyProject.WebApi.Shared;

namespace MyProject.WebApi.Features.Audit.Dtos.ListAuditEvents;

/// <summary>
/// Paginated response containing a list of audit events.
/// </summary>
public class ListAuditEventsResponse : PaginatedResponse
{
    /// <summary>
    /// The audit events for the current page.
    /// </summary>
    public IReadOnlyList<AuditEventResponse> Items { get; init; } = [];
}
