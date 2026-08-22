using System.Security.Claims;
using MyProject.Application.Identity.Constants;

namespace MyProject.Application.Identity;

/// <summary>
/// The single decision point for permission checks. A permission is granted when the subject
/// holds either the exact permission or the <see cref="AppPermissions.Wildcard"/> value.
/// There are no role-name special cases anywhere in authorization.
/// <para>
/// Multitenancy seam: to add tenant scope, add a tenant claim at token generation and extend
/// this evaluator (or scope claim values as <c>{tenantId}:{permission}</c> with
/// <c>{tenantId}:*</c>); both call sites (authorization handler and user context) stay unchanged.
/// A platform admin is then a grants-all role with null/global scope.
/// </para>
/// </summary>
public static class PermissionEvaluator
{
    /// <summary>
    /// Determines whether the principal holds the given permission, either directly
    /// or via the wildcard claim. Comparison is ordinal.
    /// </summary>
    /// <param name="principal">The claims principal, or <c>null</c> when unauthenticated.</param>
    /// <param name="permission">The permission claim value to check.</param>
    /// <returns><c>true</c> when the permission is granted; otherwise <c>false</c>.</returns>
    public static bool HasPermission(ClaimsPrincipal? principal, string permission)
    {
        if (principal is null)
        {
            return false;
        }

        return principal.Claims.Any(c =>
            string.Equals(c.Type, AppPermissions.ClaimType, StringComparison.Ordinal)
            && (string.Equals(c.Value, permission, StringComparison.Ordinal)
                || string.Equals(c.Value, AppPermissions.Wildcard, StringComparison.Ordinal)));
    }

    /// <summary>
    /// Determines whether the held permission set covers every required permission.
    /// A held set containing the wildcard covers everything. Comparison is ordinal.
    /// </summary>
    /// <param name="held">The permission values the subject holds.</param>
    /// <param name="required">The permission values that must all be covered.</param>
    /// <returns><c>true</c> when every required permission is held; otherwise <c>false</c>.</returns>
    public static bool HoldsAll(IReadOnlyCollection<string> held, IEnumerable<string> required)
    {
        var heldSet = held.ToHashSet(StringComparer.Ordinal);

        if (heldSet.Contains(AppPermissions.Wildcard))
        {
            return true;
        }

        return required.All(heldSet.Contains);
    }
}
