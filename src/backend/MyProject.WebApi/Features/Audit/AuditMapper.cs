using MyProject.Application.Features.Audit.Dtos;
using MyProject.WebApi.Features.Audit.Dtos;
using MyProject.WebApi.Features.Audit.Dtos.ListAuditEvents;

namespace MyProject.WebApi.Features.Audit;

/// <summary>
/// Maps between audit Application layer DTOs and WebApi response DTOs.
/// </summary>
internal static class AuditMapper
{
    /// <summary>
    /// Maps an <see cref="AuditEventOutput"/> to an <see cref="AuditEventResponse"/>.
    /// </summary>
    public static AuditEventResponse ToResponse(this AuditEventOutput output) => new()
    {
        Id = output.Id,
        UserId = output.UserId,
        Action = output.Action,
        TargetEntityType = output.TargetEntityType,
        TargetEntityId = output.TargetEntityId,
        Metadata = output.Metadata,
        CreatedAt = output.CreatedAt
    };

    /// <summary>
    /// Maps an <see cref="AuditEventListOutput"/> to a <see cref="ListAuditEventsResponse"/>.
    /// </summary>
    public static ListAuditEventsResponse ToResponse(this AuditEventListOutput output) => new()
    {
        Items = output.Events.Select(e => e.ToResponse()).ToList(),
        TotalCount = output.TotalCount,
        PageNumber = output.PageNumber,
        PageSize = output.PageSize
    };
}
