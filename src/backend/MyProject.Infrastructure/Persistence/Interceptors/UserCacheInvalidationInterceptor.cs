using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MyProject.Application.Caching;
using MyProject.Application.Caching.Constants;
using MyProject.Infrastructure.Features.Authentication.Models;

namespace MyProject.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Interceptor that automatically invalidates user cache when a user or their roles are modified.
/// </summary>
internal class UserCacheInvalidationInterceptor(ICacheService cacheService) : SaveChangesInterceptor
{
    /// <summary>
    /// Intercepts the saving changes operation to detect user modifications and invalidate their cache.
    /// </summary>
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context is null)
        {
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        var userIdsToInvalidate = new List<Guid>();

        var modifiedUsers = context.ChangeTracker.Entries<ApplicationUser>()
            .Where(e => e.State == EntityState.Modified)
            .Select(e => e.Entity.Id);
        
        userIdsToInvalidate.AddRange(modifiedUsers);

        var modifiedUserRoles = context.ChangeTracker.Entries<IdentityUserRole<Guid>>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Deleted || e.State == EntityState.Modified)
            .Select(e => e.Entity.UserId);

        userIdsToInvalidate.AddRange(modifiedUserRoles);

        var saveResult = await base.SavingChangesAsync(eventData, result, cancellationToken);

        if (userIdsToInvalidate.Count > 0)
        {
            await Task.WhenAll(userIdsToInvalidate
                .Distinct()
                .Select(userId => cacheService.RemoveAsync(CacheKeys.User(userId), cancellationToken)));
        }

        return saveResult;
    }
}
