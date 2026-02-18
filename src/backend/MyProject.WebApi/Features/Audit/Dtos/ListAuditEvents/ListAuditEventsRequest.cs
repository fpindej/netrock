using MyProject.WebApi.Shared;

namespace MyProject.WebApi.Features.Audit.Dtos.ListAuditEvents;

/// <summary>
/// Request parameters for listing audit events with pagination.
/// </summary>
public class ListAuditEventsRequest : PaginatedRequest;
