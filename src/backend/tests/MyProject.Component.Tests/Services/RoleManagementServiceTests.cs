using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using MyProject.Application.Caching;
using MyProject.Application.Features.Admin.Dtos;
using MyProject.Application.Identity.Constants;
using MyProject.Component.Tests.Fixtures;
using MyProject.Infrastructure.Features.Admin.Services;
using MyProject.Infrastructure.Features.Authentication.Models;
using MyProject.Infrastructure.Persistence;
using MyProject.Shared;

namespace MyProject.Component.Tests.Services;

public class RoleManagementServiceTests : IDisposable
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICacheService _cacheService;
    private readonly MyProjectDbContext _dbContext;
    private readonly RoleManagementService _sut;

    public RoleManagementServiceTests()
    {
        _roleManager = IdentityMockHelpers.CreateMockRoleManager();
        _userManager = IdentityMockHelpers.CreateMockUserManager();
        _cacheService = Substitute.For<ICacheService>();
        _dbContext = TestDbContextFactory.Create();
        var logger = Substitute.For<ILogger<RoleManagementService>>();

        _sut = new RoleManagementService(
            _roleManager, _userManager, _dbContext, _cacheService, logger);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _userManager.Dispose();
    }

    #region CreateRole

    [Fact]
    public async Task CreateRole_ValidInput_ReturnsSuccessWithGuid()
    {
        var input = new CreateRoleInput("CustomRole", "A custom role");
        _roleManager.FindByNameAsync("CustomRole").Returns((ApplicationRole?)null);
        _roleManager.CreateAsync(Arg.Any<ApplicationRole>())
            .Returns(IdentityResult.Success);

        var result = await _sut.CreateRoleAsync(input);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task CreateRole_DuplicateName_ReturnsFailure()
    {
        var input = new CreateRoleInput("ExistingRole", null);
        _roleManager.FindByNameAsync("ExistingRole")
            .Returns(new ApplicationRole { Name = "ExistingRole" });

        var result = await _sut.CreateRoleAsync(input);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Roles.RoleNameTaken, result.Error);
    }

    [Fact]
    public async Task CreateRole_SystemRoleName_ReturnsFailure()
    {
        var input = new CreateRoleInput("Admin", null);

        var result = await _sut.CreateRoleAsync(input);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Roles.SystemRoleNameReserved, result.Error);
    }

    [Theory]
    [InlineData("User")]
    [InlineData("Admin")]
    [InlineData("SuperAdmin")]
    public async Task CreateRole_AnySystemRoleName_ReturnsFailure(string systemName)
    {
        var input = new CreateRoleInput(systemName, null);

        var result = await _sut.CreateRoleAsync(input);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Roles.SystemRoleNameReserved, result.Error);
    }

    #endregion

    #region UpdateRole

    [Fact]
    public async Task UpdateRole_CustomRole_ReturnsSuccess()
    {
        var roleId = Guid.NewGuid();
        var role = new ApplicationRole { Id = roleId, Name = "CustomRole" };
        _roleManager.FindByIdAsync(roleId.ToString()).Returns(role);
        _roleManager.FindByNameAsync("NewName").Returns((ApplicationRole?)null);
        _roleManager.UpdateAsync(role).Returns(IdentityResult.Success);

        var result = await _sut.UpdateRoleAsync(roleId, new UpdateRoleInput("NewName", null));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task UpdateRole_DescriptionOnly_ReturnsSuccess()
    {
        var roleId = Guid.NewGuid();
        var role = new ApplicationRole { Id = roleId, Name = "Admin", Description = "Old" };
        _roleManager.FindByIdAsync(roleId.ToString()).Returns(role);
        _roleManager.UpdateAsync(role).Returns(IdentityResult.Success);

        var result = await _sut.UpdateRoleAsync(roleId, new UpdateRoleInput(null, "New description"));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task UpdateRole_SystemRoleRename_ReturnsFailure()
    {
        var roleId = Guid.NewGuid();
        var role = new ApplicationRole { Id = roleId, Name = "Admin" };
        _roleManager.FindByIdAsync(roleId.ToString()).Returns(role);

        var result = await _sut.UpdateRoleAsync(roleId, new UpdateRoleInput("NewAdmin", null));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Roles.SystemRoleCannotBeRenamed, result.Error);
    }

    [Fact]
    public async Task UpdateRole_NotFound_ReturnsNotFound()
    {
        _roleManager.FindByIdAsync(Arg.Any<string>()).Returns((ApplicationRole?)null);

        var result = await _sut.UpdateRoleAsync(Guid.NewGuid(), new UpdateRoleInput("Name", null));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Roles.RoleNotFound, result.Error);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task UpdateRole_NameTakenByOtherRole_ReturnsFailure()
    {
        var roleId = Guid.NewGuid();
        var role = new ApplicationRole { Id = roleId, Name = "CustomRole" };
        _roleManager.FindByIdAsync(roleId.ToString()).Returns(role);
        _roleManager.FindByNameAsync("TakenName")
            .Returns(new ApplicationRole { Id = Guid.NewGuid(), Name = "TakenName" });

        var result = await _sut.UpdateRoleAsync(roleId, new UpdateRoleInput("TakenName", null));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Roles.RoleNameTaken, result.Error);
    }

    #endregion

    #region DeleteRole

    [Fact]
    public async Task DeleteRole_SystemRole_ReturnsFailure()
    {
        var roleId = Guid.NewGuid();
        var role = new ApplicationRole { Id = roleId, Name = "Admin" };
        _roleManager.FindByIdAsync(roleId.ToString()).Returns(role);

        var result = await _sut.DeleteRoleAsync(roleId);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Roles.SystemRoleCannotBeDeleted, result.Error);
    }

    [Fact]
    public async Task DeleteRole_NotFound_ReturnsNotFound()
    {
        _roleManager.FindByIdAsync(Arg.Any<string>()).Returns((ApplicationRole?)null);

        var result = await _sut.DeleteRoleAsync(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Roles.RoleNotFound, result.Error);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task DeleteRole_WithUsers_ReturnsFailure()
    {
        var roleId = Guid.NewGuid();
        var role = new ApplicationRole { Id = roleId, Name = "CustomRole" };
        _roleManager.FindByIdAsync(roleId.ToString()).Returns(role);

        _dbContext.UserRoles.Add(new IdentityUserRole<Guid> { RoleId = roleId, UserId = Guid.NewGuid() });
        await _dbContext.SaveChangesAsync();

        var result = await _sut.DeleteRoleAsync(roleId);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Roles.RoleHasUsers, result.Error);
    }

    [Fact]
    public async Task DeleteRole_CustomNoUsers_ReturnsSuccess()
    {
        var roleId = Guid.NewGuid();
        var role = new ApplicationRole { Id = roleId, Name = "CustomRole" };
        _roleManager.FindByIdAsync(roleId.ToString()).Returns(role);
        _roleManager.DeleteAsync(role).Returns(IdentityResult.Success);

        var result = await _sut.DeleteRoleAsync(roleId);

        Assert.True(result.IsSuccess);
    }

    #endregion

    #region SetRolePermissions

    [Fact(Skip = "InMemory EF provider does not support ExecuteDeleteAsync — requires Testcontainers (issue #174)")]
    public async Task SetPermissions_ValidPermissions_ReturnsSuccess()
    {
        var roleId = Guid.NewGuid();
        var role = new ApplicationRole { Id = roleId, Name = "CustomRole" };
        _roleManager.FindByIdAsync(roleId.ToString()).Returns(role);

        var result = await _sut.SetRolePermissionsAsync(roleId,
            new SetRolePermissionsInput([AppPermissions.Users.View, AppPermissions.Users.Manage]));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task SetPermissions_SuperAdminRole_ReturnsFailure()
    {
        var roleId = Guid.NewGuid();
        var role = new ApplicationRole { Id = roleId, Name = AppRoles.SuperAdmin };
        _roleManager.FindByIdAsync(roleId.ToString()).Returns(role);

        var result = await _sut.SetRolePermissionsAsync(roleId,
            new SetRolePermissionsInput([AppPermissions.Users.View]));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Roles.SuperAdminPermissionsFixed, result.Error);
    }

    [Fact]
    public async Task SetPermissions_InvalidPermission_ReturnsFailure()
    {
        var roleId = Guid.NewGuid();
        var role = new ApplicationRole { Id = roleId, Name = "CustomRole" };
        _roleManager.FindByIdAsync(roleId.ToString()).Returns(role);

        var result = await _sut.SetRolePermissionsAsync(roleId,
            new SetRolePermissionsInput(["invalid.permission"]));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Roles.InvalidPermission, result.Error);
    }

    [Fact]
    public async Task SetPermissions_NotFound_ReturnsNotFound()
    {
        _roleManager.FindByIdAsync(Arg.Any<string>()).Returns((ApplicationRole?)null);

        var result = await _sut.SetRolePermissionsAsync(Guid.NewGuid(),
            new SetRolePermissionsInput([AppPermissions.Users.View]));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Roles.RoleNotFound, result.Error);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    #endregion

    #region GetAllPermissions

    [Fact]
    public void GetAllPermissions_ReturnsGroupedPermissions()
    {
        var permissions = _sut.GetAllPermissions();

        Assert.NotEmpty(permissions);
        Assert.Contains(permissions, g => g.Category == "Users");
        Assert.Contains(permissions, g => g.Category == "Roles");
        Assert.Contains(permissions, g => g.Category == "Jobs");
    }

    #endregion

    #region GetRoleDetail

    [Fact]
    public async Task GetRoleDetail_Found_ReturnsSuccess()
    {
        var roleId = Guid.NewGuid();
        var role = new ApplicationRole { Id = roleId, Name = "CustomRole", Description = "A role" };
        _roleManager.FindByIdAsync(roleId.ToString()).Returns(role);

        var result = await _sut.GetRoleDetailAsync(roleId);

        Assert.True(result.IsSuccess);
        Assert.Equal("CustomRole", result.Value.Name);
        Assert.Equal("A role", result.Value.Description);
    }

    [Fact]
    public async Task GetRoleDetail_NotFound_ReturnsNotFound()
    {
        _roleManager.FindByIdAsync(Arg.Any<string>()).Returns((ApplicationRole?)null);

        var result = await _sut.GetRoleDetailAsync(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Roles.RoleNotFound, result.Error);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    #endregion
}
