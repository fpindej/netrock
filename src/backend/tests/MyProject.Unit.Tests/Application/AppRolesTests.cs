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
    public void All_ShouldContainSuperAdmin()
    {
        Assert.Contains(AppRoles.SuperAdmin, AppRoles.All);
    }

    [Fact]
    public void All_ShouldHaveAtLeastThreeRoles()
    {
        Assert.True(AppRoles.All.Count >= 3);
    }

    [Fact]
    public void GetRoleRank_SuperAdmin_ShouldReturn3()
    {
        Assert.Equal(3, AppRoles.GetRoleRank(AppRoles.SuperAdmin));
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
        Assert.Equal(3, AppRoles.GetHighestRank([AppRoles.SuperAdmin]));
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
        Assert.Equal("SuperAdmin", AppRoles.SuperAdmin);
    }
}
