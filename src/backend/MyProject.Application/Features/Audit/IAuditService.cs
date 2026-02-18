using MyProject.Application.Features.Audit.Dtos;

namespace MyProject.Application.Features.Audit;

/// <summary>
/// Provides audit logging and retrieval capabilities.
/// <para>
/// <see cref="LogAsync"/> never throws — failures are silently logged to prevent
/// audit instrumentation from breaking the main operation.
/// </para>
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Records an audit event. Failures are swallowed and logged; this method never throws.
    /// </summary>
    /// <param name="action">The action constant from <see cref="AuditActions"/>.</param>
    /// <param name="userId">The user who performed the action, or <c>null</c> for anonymous events.</param>
    /// <param name="targetEntityType">The type of entity targeted by the action.</param>
    /// <param name="targetEntityId">The ID of the entity targeted by the action.</param>
    /// <param name="metadata">Optional JSON metadata providing additional context.</param>
    /// <param name="ct">A cancellation token.</param>
    Task LogAsync(
        string action,
        Guid? userId = null,
        string? targetEntityType = null,
        Guid? targetEntityId = null,
        string? metadata = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a paginated list of audit events for a specific user, ordered by most recent first.
    /// </summary>
    /// <param name="userId">The user whose audit events to retrieve.</param>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A paginated list of audit events.</returns>
    Task<AuditEventListOutput> GetUserAuditEventsAsync(
        Guid userId,
        int pageNumber,
        int pageSize,
        CancellationToken ct = default);
}
