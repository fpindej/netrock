using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using MyProject.Application.Caching;
using MyProject.Application.Features.Admin.Dtos;
using MyProject.Application.Identity.Constants;
using MyProject.Component.Tests.Fixtures;
using MyProject.Infrastructure.Features.Admin.Services;
using MyProject.Infrastructure.Features.Authentication.Models;
using MyProject.Infrastructure.Persistence;
using MyProject.Shared;

namespace MyProject.Component.Tests.Services;

public class AdminServiceTests : IDisposable
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ICacheService _cacheService;
    private readonly FakeTimeProvider _timeProvider;
    private readonly MyProjectDbContext _dbContext;
    private readonly AdminService _sut;

    private readonly Guid _callerId = Guid.NewGuid();
    private readonly Guid _targetId = Guid.NewGuid();

    public AdminServiceTests()
    {
        _userManager = IdentityMockHelpers.CreateMockUserManager();
        _roleManager = IdentityMockHelpers.CreateMockRoleManager();
        _cacheService = Substitute.For<ICacheService>();
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2025, 1, 15, 12, 0, 0, TimeSpan.Zero));
        _dbContext = TestDbContextFactory.Create();
        var logger = Substitute.For<ILogger<AdminService>>();

        _sut = new AdminService(
            _userManager, _roleManager, _dbContext, _cacheService, _timeProvider, logger);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _userManager.Dispose();
    }

    private void SetupCallerAsAdmin()
    {
        var caller = new ApplicationUser { Id = _callerId, UserName = "admin@test.com" };
        _userManager.FindByIdAsync(_callerId.ToString()).Returns(caller);
        _userManager.GetRolesAsync(caller).Returns(new List<string> { AppRoles.Admin });
    }

    private ApplicationUser SetupTargetAsUser()
    {
        var target = new ApplicationUser { Id = _targetId, UserName = "user@test.com" };
        _userManager.FindByIdAsync(_targetId.ToString()).Returns(target);
        _userManager.GetRolesAsync(target).Returns(new List<string> { AppRoles.User });
        return target;
    }

    #region AssignRole

    [Fact]
    public async Task AssignRole_Valid_ReturnsSuccess()
    {
        SetupCallerAsAdmin();
        var target = SetupTargetAsUser();
        _roleManager.FindByNameAsync("User").Returns(new ApplicationRole { Name = "User" });
        _userManager.IsInRoleAsync(target, "User").Returns(false);
        _userManager.AddToRoleAsync(target, "User").Returns(IdentityResult.Success);

        var result = await _sut.AssignRoleAsync(_callerId, _targetId, new AssignRoleInput("User"));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task AssignRole_RoleDoesNotExist_ReturnsFailure()
    {
        _roleManager.FindByNameAsync("NonExistent").Returns((ApplicationRole?)null);

        var result = await _sut.AssignRoleAsync(_callerId, _targetId, new AssignRoleInput("NonExistent"));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task AssignRole_UserNotFound_ReturnsNotFound()
    {
        _roleManager.FindByNameAsync("User").Returns(new ApplicationRole { Name = "User" });
        _userManager.FindByIdAsync(_targetId.ToString()).Returns((ApplicationUser?)null);

        var result = await _sut.AssignRoleAsync(_callerId, _targetId, new AssignRoleInput("User"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Admin.UserNotFound, result.Error);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task AssignRole_HigherRoleThanCaller_ReturnsFailure()
    {
        SetupCallerAsAdmin();
        SetupTargetAsUser();
        _roleManager.FindByNameAsync("Admin").Returns(new ApplicationRole { Name = "Admin" });

        var result = await _sut.AssignRoleAsync(_callerId, _targetId, new AssignRoleInput("Admin"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Admin.RoleAssignAboveRank, result.Error);
    }

    #endregion

    #region RemoveRole

    [Fact]
    public async Task RemoveRole_SelfRemoval_ReturnsFailure()
    {
        _roleManager.FindByNameAsync("User").Returns(new ApplicationRole { Name = "User" });
        var user = new ApplicationUser { Id = _callerId, UserName = "self@test.com" };
        _userManager.FindByIdAsync(_callerId.ToString()).Returns(user);

        var result = await _sut.RemoveRoleAsync(_callerId, _callerId, "User");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Admin.RoleSelfRemove, result.Error);
    }

    [Fact]
    public async Task RemoveRole_RoleAboveCallerRank_ReturnsFailure()
    {
        SetupCallerAsAdmin();
        var target = SetupTargetAsUser();
        _roleManager.FindByNameAsync("Admin").Returns(new ApplicationRole { Name = "Admin" });
        _userManager.IsInRoleAsync(target, "Admin").Returns(true);

        var result = await _sut.RemoveRoleAsync(_callerId, _targetId, "Admin");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Admin.RoleRemoveAboveRank, result.Error);
    }

    #endregion

    #region LockUser

    [Fact]
    public async Task LockUser_Valid_ReturnsSuccess()
    {
        SetupCallerAsAdmin();
        var target = SetupTargetAsUser();
        _userManager.SetLockoutEndDateAsync(target, Arg.Any<DateTimeOffset>())
            .Returns(IdentityResult.Success);

        var result = await _sut.LockUserAsync(_callerId, _targetId);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task LockUser_SelfLock_ReturnsFailure()
    {
        var user = new ApplicationUser { Id = _callerId, UserName = "admin@test.com" };
        _userManager.FindByIdAsync(_callerId.ToString()).Returns(user);

        var result = await _sut.LockUserAsync(_callerId, _callerId);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Admin.LockSelfAction, result.Error);
    }

    [Fact]
    public async Task LockUser_UserNotFound_ReturnsNotFound()
    {
        _userManager.FindByIdAsync(_targetId.ToString()).Returns((ApplicationUser?)null);

        var result = await _sut.LockUserAsync(_callerId, _targetId);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Admin.UserNotFound, result.Error);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    #endregion

    #region UnlockUser

    [Fact]
    public async Task UnlockUser_Valid_ReturnsSuccess()
    {
        SetupCallerAsAdmin();
        var target = SetupTargetAsUser();
        _userManager.SetLockoutEndDateAsync(target, null).Returns(IdentityResult.Success);
        _userManager.ResetAccessFailedCountAsync(target).Returns(IdentityResult.Success);

        var result = await _sut.UnlockUserAsync(_callerId, _targetId);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task UnlockUser_UserNotFound_ReturnsNotFound()
    {
        _userManager.FindByIdAsync(_targetId.ToString()).Returns((ApplicationUser?)null);

        var result = await _sut.UnlockUserAsync(_callerId, _targetId);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Admin.UserNotFound, result.Error);
    }

    #endregion

    #region DeleteUser

    [Fact]
    public async Task DeleteUser_SelfDelete_ReturnsFailure()
    {
        var user = new ApplicationUser { Id = _callerId, UserName = "admin@test.com" };
        _userManager.FindByIdAsync(_callerId.ToString()).Returns(user);

        var result = await _sut.DeleteUserAsync(_callerId, _callerId);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Admin.DeleteSelfAction, result.Error);
    }

    [Fact]
    public async Task DeleteUser_UserNotFound_ReturnsNotFound()
    {
        _userManager.FindByIdAsync(_targetId.ToString()).Returns((ApplicationUser?)null);

        var result = await _sut.DeleteUserAsync(_callerId, _targetId);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Admin.UserNotFound, result.Error);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    #endregion

    #region GetUserById

    [Fact]
    public async Task GetUserById_NotFound_ReturnsNotFound()
    {
        _userManager.FindByIdAsync(_targetId.ToString()).Returns((ApplicationUser?)null);

        var result = await _sut.GetUserByIdAsync(_targetId);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Admin.UserNotFound, result.Error);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    #endregion
}
