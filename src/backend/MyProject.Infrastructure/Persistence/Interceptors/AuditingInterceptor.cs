using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MyProject.Application.Identity;
using MyProject.Domain.Entities;

namespace MyProject.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Interceptor that automatically sets auditing properties (CreatedBy, CreatedAt, UpdatedBy, UpdatedAt)
/// on entities implementing <see cref="BaseEntity"/> before they are saved to the database.
/// </summary>
internal class AuditingInterceptor(
    IUserContext userContext,
    TimeProvider timeProvider) : SaveChangesInterceptor
{
    /// <summary>
    /// Called at the start of <see cref="DbContext.SaveChanges"/>.
    /// </summary>
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateAuditableEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <summary>
    /// Called at the start of <see cref="DbContext.SaveChangesAsync"/>.
    /// </summary>
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        UpdateAuditableEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateAuditableEntities(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var entries = context.ChangeTracker.Entries<BaseEntity>();
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var userId = userContext.UserId;

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Property(e => e.CreatedAt).CurrentValue = utcNow;
                    entry.Property(e => e.CreatedBy).CurrentValue = userId;
                    break;
                case EntityState.Modified:
                    entry.Property(e => e.UpdatedAt).CurrentValue = utcNow;
                    entry.Property(e => e.UpdatedBy).CurrentValue = userId;

                    if (entry.Property(e => e.IsDeleted).IsModified)
                    {
                        if (entry.Entity.IsDeleted)
                        {
                            entry.Property(e => e.DeletedBy).CurrentValue = userId;
                            entry.Property(e => e.DeletedAt).CurrentValue = utcNow;
                        }
                        else
                        {
                            entry.Property(e => e.DeletedBy).CurrentValue = null;
                            entry.Property(e => e.DeletedAt).CurrentValue = null;
                        }
                    }

                    break;
            }
        }
    }
}
