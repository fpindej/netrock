using Microsoft.EntityFrameworkCore;
using MyProject.Application.Identity;
using MyProject.Application.Identity.Constants;
using MyProject.Infrastructure.Persistence;
using MyProject.Shared;

namespace MyProject.Infrastructure.Features.Admin.Services;

/// <summary>
/// Shared guard that prevents permission escalation: a caller may only grant permissions
/// they hold themselves. Held permissions are resolved from the caller's roles in a single
/// query; a role with <c>GrantsAllPermissions</c> counts as holding the wildcard and
/// therefore every permission. The decision is made by <see cref="PermissionEvaluator.HoldsAll"/>.
/// </summary>
internal class PermissionEscalationGuard(MyProjectDbContext dbContext)
{
    /// <summary>
    /// Verifies that the caller holds every permission in <paramref name="requiredPermissions"/>.
    /// Returns <see cref="Result.Success()"/> when the set is empty or fully covered; otherwise
    /// a forbidden failure carrying <paramref name="failureError"/> so each call site keeps
    /// its own stable error code.
    /// </summary>
    /// <param name="callerUserId">The id of the user attempting the grant.</param>
    /// <param name="requiredPermissions">The permissions being granted.</param>
    /// <param name="failureError">The error returned when the caller does not hold all permissions.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task<Result> EnsureCallerHoldsAllAsync(Guid callerUserId,
        IReadOnlyCollection<string> requiredPermissions, Error failureError,
        CancellationToken cancellationToken = default)
    {
        if (requiredPermissions.Count == 0)
        {
            return Result.Success();
        }

        var callerRoles = await dbContext.UserRoles
            .Where(ur => ur.UserId == callerUserId)
            .Join(dbContext.Roles,
                ur => ur.RoleId,
                r => r.Id,
                (ur, r) => new
                {
                    r.GrantsAllPermissions,
                    Permissions = dbContext.RoleClaims
                        .Where(rc => rc.RoleId == r.Id && rc.ClaimType == AppPermissions.ClaimType)
                        .Select(rc => rc.ClaimValue!)
                        .ToList()
                })
            .ToListAsync(cancellationToken);

        var heldPermissions = callerRoles.SelectMany(r => r.Permissions).ToList();
        if (callerRoles.Any(r => r.GrantsAllPermissions))
        {
            heldPermissions.Add(AppPermissions.Wildcard);
        }

        return PermissionEvaluator.HoldsAll(heldPermissions, requiredPermissions)
            ? Result.Success()
            : Result.Failure(failureError, ErrorType.Forbidden);
    }
}
