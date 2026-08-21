using Microsoft.AspNetCore.Authorization;
using MyProject.Application.Identity;

namespace MyProject.WebApi.Authorization;

/// <summary>
/// Handles <see cref="PermissionRequirement"/> by delegating to
/// <see cref="PermissionEvaluator.HasPermission"/>: a uniform claims check where the
/// wildcard claim of grants-all roles covers every permission. No role-name special cases.
/// </summary>
internal class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    /// <inheritdoc />
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (PermissionEvaluator.HasPermission(context.User, requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
