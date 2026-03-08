using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Test.Application.Features.Audit;
using Test.Application.Features.Audit.Dtos;
using Test.Infrastructure.Features.Audit.Models;
using Test.Infrastructure.Persistence;
using Test.Infrastructure.Persistence.Extensions;

namespace Test.Infrastructure.Features.Audit.Services;

/// <summary>
/// Append-only audit log service. <see cref="LogAsync"/> never throws to prevent
/// audit instrumentation from breaking the main operation.
/// </summary>
internal class AuditService(
    TestDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<AuditService> logger) : IAuditService
{
    /// <inheritdoc />
    public async Task LogAsync(
        string action,
        Guid? userId = null,
        string? targetEntityType = null,
        Guid? targetEntityId = null,
        string? metadata = null,
        CancellationToken ct = default)
    {
        try
        {
            var auditEvent = new AuditEvent
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Action = action,
                TargetEntityType = targetEntityType,
                TargetEntityId = targetEntityId,
                Metadata = metadata,
                CreatedAt = timeProvider.GetUtcNow().UtcDateTime
            };

            dbContext.AuditEvents.Add(auditEvent);
            await dbContext.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to log audit event '{Action}' for user '{UserId}'", action, userId);
        }
    }

    /// <inheritdoc />
    public async Task<AuditEventListOutput> GetUserAuditEventsAsync(
        Guid userId,
        int pageNumber,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = dbContext.AuditEvents
            .AsNoTracking()
            .Where(e => e.UserId == userId || e.TargetEntityId == userId);

        var totalCount = await query.CountAsync(ct);

        var events = await query
            .OrderByDescending(e => e.CreatedAt)
            .Paginate(pageNumber, pageSize)
            .Select(e => new AuditEventOutput(
                e.Id,
                e.UserId,
                e.Action,
                e.TargetEntityType,
                e.TargetEntityId,
                e.Metadata,
                e.CreatedAt))
            .ToListAsync(ct);

        return new AuditEventListOutput(events, totalCount, pageNumber, pageSize);
    }
}
