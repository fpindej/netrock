using MyProject.Application.Identity.Constants;

namespace MyProject.Unit.Tests.Application;

public class AppPermissionsTests
{
    [Fact]
    public void All_ShouldBeNonEmpty()
    {
        Assert.NotEmpty(AppPermissions.All);
    }

    [Fact]
    public void All_ShouldHaveNoDuplicates()
    {
        Assert.Equal(AppPermissions.All.Count, AppPermissions.All.Distinct().Count());
    }

    [Fact]
    public void ByCategory_ShouldContainUsersCategory()
    {
        Assert.True(AppPermissions.ByCategory.ContainsKey("Users"));
    }

    [Fact]
    public void ByCategory_ShouldContainRolesCategory()
    {
        Assert.True(AppPermissions.ByCategory.ContainsKey("Roles"));
    }

    [Fact]
    public void ByCategory_ShouldContainJobsCategory()
    {
        Assert.True(AppPermissions.ByCategory.ContainsKey("Jobs"));
    }

    [Fact]
    public void ClaimType_ShouldBePermission()
    {
        Assert.Equal("permission", AppPermissions.ClaimType);
    }

    [Fact]
    public void UsersPermissions_ShouldExist()
    {
        Assert.Equal("users.view", AppPermissions.Users.View);
        Assert.Equal("users.manage", AppPermissions.Users.Manage);
        Assert.Equal("users.assign_roles", AppPermissions.Users.AssignRoles);
    }

    [Fact]
    public void RolesPermissions_ShouldExist()
    {
        Assert.Equal("roles.view", AppPermissions.Roles.View);
        Assert.Equal("roles.manage", AppPermissions.Roles.Manage);
    }

    [Fact]
    public void JobsPermissions_ShouldExist()
    {
        Assert.Equal("jobs.view", AppPermissions.Jobs.View);
        Assert.Equal("jobs.manage", AppPermissions.Jobs.Manage);
    }

    [Fact]
    public void All_ShouldContainEveryDefinedPermission()
    {
        Assert.Contains(AppPermissions.Users.View, AppPermissions.All);
        Assert.Contains(AppPermissions.Users.Manage, AppPermissions.All);
        Assert.Contains(AppPermissions.Users.AssignRoles, AppPermissions.All);
        Assert.Contains(AppPermissions.Roles.View, AppPermissions.All);
        Assert.Contains(AppPermissions.Roles.Manage, AppPermissions.All);
        Assert.Contains(AppPermissions.Jobs.View, AppPermissions.All);
        Assert.Contains(AppPermissions.Jobs.Manage, AppPermissions.All);
    }

    [Fact]
    public void ByCategory_TotalPermissions_ShouldMatchAll()
    {
        var totalFromCategories = AppPermissions.ByCategory.Values.Sum(list => list.Count);

        Assert.Equal(AppPermissions.All.Count, totalFromCategories);
    }
}
