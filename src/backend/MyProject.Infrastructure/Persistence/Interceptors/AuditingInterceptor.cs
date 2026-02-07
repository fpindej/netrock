using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MyProject.Application.Identity;
using MyProject.Domain.Entities;

namespace MyProject.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Interceptor that automatically sets auditing properties on entities implementing <see cref="BaseEntity"/>
/// before they are saved to the database.
/// <list type="bullet">
///     <item><description>Added entities: Sets <c>CreatedAt</c> and <c>CreatedBy</c></description></item>
///     <item><description>Modified entities: Sets <c>UpdatedAt</c> and <c>UpdatedBy</c></description></item>
///     <item><description>Soft-deleted entities: Sets <c>DeletedAt</c> and <c>DeletedBy</c></description></item>
///     <item><description>Restored entities: Clears <c>DeletedAt</c> and <c>DeletedBy</c></description></item>
/// </list>
/// </summary>
/// <remarks>
/// The <c>*By</c> fields will be <c>null</c> when no user is authenticated (e.g., background jobs, system operations).
/// This is expected behavior - entities created or modified by the system will have <c>null</c> user references.
/// <para>Pattern documented in src/backend/AGENTS.md — update both when changing.</para>
/// </remarks>
internal class AuditingInterceptor(
    IUserContext userContext,
    TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ApplyAuditingFields(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditingFields(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAuditingFields(DbContext? context)
    {
        if (context is null) return;

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var userId = userContext.UserId;

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>()) ApplyAuditingFields(entry, utcNow, userId);
    }

    private static void ApplyAuditingFields(EntityEntry<BaseEntity> entry, DateTime utcNow, Guid? userId)
    {
        switch (entry.State)
        {
            case EntityState.Added:
                SetCreatedFields(entry, utcNow, userId);
                break;

            case EntityState.Modified when IsSoftDelete(entry):
                SetUpdatedFields(entry, utcNow, userId);
                SetDeletedFields(entry, utcNow, userId);
                break;

            case EntityState.Modified when IsRestore(entry):
                SetUpdatedFields(entry, utcNow, userId);
                ClearDeletedFields(entry);
                break;

            case EntityState.Modified:
                SetUpdatedFields(entry, utcNow, userId);
                break;
        }
    }

    private static bool IsSoftDelete(EntityEntry<BaseEntity> entry)
    {
        var isDeletedProperty = entry.Property(e => e.IsDeleted);
        return isDeletedProperty is { IsModified: true, OriginalValue: false, CurrentValue: true };
    }

    private static bool IsRestore(EntityEntry<BaseEntity> entry)
    {
        var isDeletedProperty = entry.Property(e => e.IsDeleted);
        return isDeletedProperty is { IsModified: true, OriginalValue: true, CurrentValue: false };
    }

    private static void SetCreatedFields(EntityEntry<BaseEntity> entry, DateTime utcNow, Guid? userId)
    {
        entry.Property(e => e.CreatedAt).CurrentValue = utcNow;
        entry.Property(e => e.CreatedBy).CurrentValue = userId;
    }

    private static void SetUpdatedFields(EntityEntry<BaseEntity> entry, DateTime utcNow, Guid? userId)
    {
        entry.Property(e => e.UpdatedAt).CurrentValue = utcNow;
        entry.Property(e => e.UpdatedBy).CurrentValue = userId;
    }

    private static void SetDeletedFields(EntityEntry<BaseEntity> entry, DateTime utcNow, Guid? userId)
    {
        entry.Property(e => e.DeletedAt).CurrentValue = utcNow;
        entry.Property(e => e.DeletedBy).CurrentValue = userId;
    }

    private static void ClearDeletedFields(EntityEntry<BaseEntity> entry)
    {
        entry.Property(e => e.DeletedAt).CurrentValue = null;
        entry.Property(e => e.DeletedBy).CurrentValue = null;
    }
}
