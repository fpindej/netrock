using MyProject.Infrastructure.Features.Authentication.Models;

namespace MyProject.Infrastructure.Features.Audit.Models;

/// <summary>
/// Represents an append-only audit log entry recording a user or system action.
/// </summary>
public class AuditEvent
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who performed the action,
    /// or <c>null</c> for anonymous events (e.g. failed login by unknown email).
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Gets or sets the action that was performed (see <see cref="Application.Features.Audit.AuditActions"/>).
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of entity targeted by the action (e.g. "User", "Role").
    /// </summary>
    public string? TargetEntityType { get; set; }

    /// <summary>
    /// Gets or sets the ID of the entity targeted by the action.
    /// </summary>
    public Guid? TargetEntityId { get; set; }

    /// <summary>
    /// Gets or sets optional JSON metadata providing additional context.
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the event occurred.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the user who performed the action.
    /// </summary>
    public ApplicationUser? User { get; set; }
}
