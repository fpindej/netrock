using MyProject.Application.Identity.Constants;

namespace MyProject.Unit.Tests.Application;

public class AppRolesTests
{
    [Fact]
    public void All_ShouldContainUser()
    {
        Assert.Contains(AppRoles.User, AppRoles.All);
    }

    [Fact]
    public void All_ShouldContainAdmin()
    {
        Assert.Contains(AppRoles.Admin, AppRoles.All);
    }

    [Fact]
    public void All_ShouldContainSuperuser()
    {
        Assert.Contains(AppRoles.Superuser, AppRoles.All);
    }

    [Fact]
    public void All_ShouldHaveAtLeastThreeRoles()
    {
        Assert.True(AppRoles.All.Count >= 3);
    }

    [Fact]
    public void GetRoleRank_Superuser_ShouldReturn3()
    {
        Assert.Equal(3, AppRoles.GetRoleRank(AppRoles.Superuser));
    }

    [Fact]
    public void GetRoleRank_Admin_ShouldReturn2()
    {
        Assert.Equal(2, AppRoles.GetRoleRank(AppRoles.Admin));
    }

    [Fact]
    public void GetRoleRank_User_ShouldReturn1()
    {
        Assert.Equal(1, AppRoles.GetRoleRank(AppRoles.User));
    }

    [Fact]
    public void GetRoleRank_Unknown_ShouldReturn0()
    {
        Assert.Equal(0, AppRoles.GetRoleRank("CustomRole"));
    }

    [Fact]
    public void GetHighestRank_ShouldReturnMaxRank()
    {
        var roles = new[] { AppRoles.User, AppRoles.Admin };

        Assert.Equal(2, AppRoles.GetHighestRank(roles));
    }

    [Fact]
    public void GetHighestRank_SingleRole_ShouldReturnThatRank()
    {
        Assert.Equal(3, AppRoles.GetHighestRank([AppRoles.Superuser]));
    }

    [Fact]
    public void GetHighestRank_EmptyCollection_ShouldReturn0()
    {
        Assert.Equal(0, AppRoles.GetHighestRank([]));
    }

    [Fact]
    public void GetHighestRank_OnlyCustomRoles_ShouldReturn0()
    {
        Assert.Equal(0, AppRoles.GetHighestRank(["CustomA", "CustomB"]));
    }

    [Fact]
    public void RoleConstants_ShouldHaveExpectedValues()
    {
        Assert.Equal("User", AppRoles.User);
        Assert.Equal("Admin", AppRoles.Admin);
        Assert.Equal("Superuser", AppRoles.Superuser);
    }

    [Fact]
    public void Definitions_ShouldCoverAllRoleConstants()
    {
        var definedNames = AppRoles.Definitions
            .OrderByDescending(d => d.Rank)
            .Select(d => d.Name)
            .ToList();

        Assert.Equal([AppRoles.Superuser, AppRoles.Admin, AppRoles.User], definedNames);
        Assert.Equal(definedNames.OrderBy(n => n), AppRoles.All.OrderBy(n => n));
    }

    [Fact]
    public void Definitions_Superuser_ShouldGrantAllPermissionsWithNoDefaults()
    {
        var superuser = AppRoles.Definitions.Single(d => d.Name == AppRoles.Superuser);

        Assert.Equal(3, superuser.Rank);
        Assert.True(superuser.IsSystem);
        Assert.True(superuser.GrantsAllPermissions);
        Assert.Empty(superuser.DefaultPermissions);
    }

    [Fact]
    public void Definitions_Admin_ShouldHaveDefaultPermissionsWithoutRolesManage()
    {
        var admin = AppRoles.Definitions.Single(d => d.Name == AppRoles.Admin);

        Assert.Equal(2, admin.Rank);
        Assert.True(admin.IsSystem);
        Assert.False(admin.GrantsAllPermissions);
        Assert.Contains(AppPermissions.Users.View, admin.DefaultPermissions);
        Assert.Contains(AppPermissions.Users.Manage, admin.DefaultPermissions);
        Assert.Contains(AppPermissions.Users.AssignRoles, admin.DefaultPermissions);
        Assert.Contains(AppPermissions.Roles.View, admin.DefaultPermissions);
        Assert.DoesNotContain(AppPermissions.Roles.Manage, admin.DefaultPermissions);
    }

    [Fact]
    public void Definitions_User_ShouldBeSystemRoleWithoutPermissions()
    {
        var user = AppRoles.Definitions.Single(d => d.Name == AppRoles.User);

        Assert.Equal(1, user.Rank);
        Assert.True(user.IsSystem);
        Assert.False(user.GrantsAllPermissions);
        Assert.Empty(user.DefaultPermissions);
    }

    [Fact]
    public void Definitions_DefaultPermissions_ShouldAllBeKnownPermissions()
    {
        foreach (var definition in AppRoles.Definitions)
        {
            Assert.All(definition.DefaultPermissions, p => Assert.Contains(p, AppPermissions.All));
        }
    }
}
