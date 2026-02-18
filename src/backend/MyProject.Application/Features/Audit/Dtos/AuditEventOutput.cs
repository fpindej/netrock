namespace MyProject.Application.Features.Audit.Dtos;

/// <summary>
/// Output representing a single audit event.
/// </summary>
/// <param name="Id">The unique identifier of the audit event.</param>
/// <param name="UserId">The user who performed the action, or <c>null</c> for anonymous events.</param>
/// <param name="Action">The action that was performed (see <see cref="AuditActions"/>).</param>
/// <param name="TargetEntityType">The type of entity targeted by the action, or <c>null</c>.</param>
/// <param name="TargetEntityId">The ID of the entity targeted by the action, or <c>null</c>.</param>
/// <param name="Metadata">Optional JSON metadata providing additional context.</param>
/// <param name="CreatedAt">The UTC timestamp when the event occurred.</param>
public record AuditEventOutput(
    Guid Id,
    Guid? UserId,
    string Action,
    string? TargetEntityType,
    Guid? TargetEntityId,
    string? Metadata,
    DateTime CreatedAt
);
