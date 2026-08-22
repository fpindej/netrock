using System.Security.Claims;
using MyProject.Application.Identity;
using MyProject.Application.Identity.Constants;

namespace MyProject.Unit.Tests.Application;

public class PermissionEvaluatorTests
{
    private static ClaimsPrincipal PrincipalWithPermissions(params string[] permissions)
    {
        var claims = permissions.Select(p => new Claim(AppPermissions.ClaimType, p));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    [Fact]
    public void HasPermission_NullPrincipal_ReturnsFalse()
    {
        Assert.False(PermissionEvaluator.HasPermission(null, AppPermissions.Users.View));
    }

    [Fact]
    public void HasPermission_ExactMatch_ReturnsTrue()
    {
        var principal = PrincipalWithPermissions(AppPermissions.Users.View);

        Assert.True(PermissionEvaluator.HasPermission(principal, AppPermissions.Users.View));
    }

    [Fact]
    public void HasPermission_MissingPermission_ReturnsFalse()
    {
        var principal = PrincipalWithPermissions(AppPermissions.Users.View);

        Assert.False(PermissionEvaluator.HasPermission(principal, AppPermissions.Users.Manage));
    }

    [Fact]
    public void HasPermission_WildcardClaim_GrantsEveryPermission()
    {
        var principal = PrincipalWithPermissions(AppPermissions.Wildcard);

        Assert.All(AppPermissions.All, p => Assert.True(PermissionEvaluator.HasPermission(principal, p)));
    }

    [Fact]
    public void HasPermission_SuperuserRoleWithoutClaims_ReturnsFalse()
    {
        // No role-name special case: a role claim alone grants nothing.
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Role, AppRoles.Superuser)], "Test");
        var principal = new ClaimsPrincipal(identity);

        Assert.False(PermissionEvaluator.HasPermission(principal, AppPermissions.Users.View));
    }

    [Fact]
    public void HasPermission_ComparisonIsOrdinal_CaseMismatchDenied()
    {
        var principal = PrincipalWithPermissions("Users.View");

        Assert.False(PermissionEvaluator.HasPermission(principal, AppPermissions.Users.View));
    }

    [Fact]
    public void HoldsAll_HeldContainsWildcard_ReturnsTrue()
    {
        Assert.True(PermissionEvaluator.HoldsAll([AppPermissions.Wildcard], AppPermissions.All));
    }

    [Fact]
    public void HoldsAll_AllRequiredHeld_ReturnsTrue()
    {
        string[] held = [AppPermissions.Users.View, AppPermissions.Users.Manage];

        Assert.True(PermissionEvaluator.HoldsAll(held, [AppPermissions.Users.View]));
    }

    [Fact]
    public void HoldsAll_MissingRequired_ReturnsFalse()
    {
        string[] held = [AppPermissions.Users.View];

        Assert.False(PermissionEvaluator.HoldsAll(held, [AppPermissions.Users.View, AppPermissions.Roles.Manage]));
    }

    [Fact]
    public void HoldsAll_EmptyRequired_ReturnsTrue()
    {
        Assert.True(PermissionEvaluator.HoldsAll([], []));
    }
}
