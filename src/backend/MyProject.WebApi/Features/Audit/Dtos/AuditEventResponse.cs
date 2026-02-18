using JetBrains.Annotations;

namespace MyProject.WebApi.Features.Audit.Dtos;

/// <summary>
/// Represents a single audit event in API responses.
/// </summary>
public class AuditEventResponse
{
    /// <summary>
    /// The unique identifier of the audit event.
    /// </summary>
    public Guid Id { [UsedImplicitly] get; [UsedImplicitly] init; }

    /// <summary>
    /// The user who performed the action, or null for anonymous events.
    /// </summary>
    public Guid? UserId { [UsedImplicitly] get; [UsedImplicitly] init; }

    /// <summary>
    /// The action that was performed.
    /// </summary>
    public string Action { [UsedImplicitly] get; [UsedImplicitly] init; } = string.Empty;

    /// <summary>
    /// The type of entity targeted by the action.
    /// </summary>
    public string? TargetEntityType { [UsedImplicitly] get; [UsedImplicitly] init; }

    /// <summary>
    /// The ID of the entity targeted by the action.
    /// </summary>
    public Guid? TargetEntityId { [UsedImplicitly] get; [UsedImplicitly] init; }

    /// <summary>
    /// Optional JSON metadata providing additional context.
    /// </summary>
    public string? Metadata { [UsedImplicitly] get; [UsedImplicitly] init; }

    /// <summary>
    /// The UTC timestamp when the event occurred.
    /// </summary>
    public DateTime CreatedAt { [UsedImplicitly] get; [UsedImplicitly] init; }
}
