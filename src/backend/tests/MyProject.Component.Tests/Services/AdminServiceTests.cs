using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using MyProject.Application.Caching;
using MyProject.Application.Caching.Constants;
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

    private ApplicationUser SetupCallerAsAdmin()
    {
        var caller = new ApplicationUser { Id = _callerId, UserName = "admin@test.com" };
        _userManager.FindByIdAsync(_callerId.ToString()).Returns(caller);
        _userManager.GetRolesAsync(caller).Returns(new List<string> { AppRoles.Admin });
        return caller;
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

    [Fact]
    public async Task AssignRole_UserAlreadyHasRole_ReturnsFailure()
    {
        SetupCallerAsAdmin();
        var target = SetupTargetAsUser();
        _roleManager.FindByNameAsync("User").Returns(new ApplicationRole { Name = "User" });
        _userManager.IsInRoleAsync(target, "User").Returns(true);

        var result = await _sut.AssignRoleAsync(_callerId, _targetId, new AssignRoleInput("User"));

        Assert.True(result.IsFailure);
        Assert.Contains("already has", result.Error);
    }

    #endregion

    #region RemoveRole

    [Fact]
    public async Task RemoveRole_Valid_ReturnsSuccess()
    {
        SetupCallerAsAdmin();
        var target = SetupTargetAsUser();
        _roleManager.FindByNameAsync("User").Returns(new ApplicationRole { Name = "User" });
        _userManager.IsInRoleAsync(target, "User").Returns(true);
        _userManager.RemoveFromRoleAsync(target, "User").Returns(IdentityResult.Success);

        var result = await _sut.RemoveRoleAsync(_callerId, _targetId, "User");

        Assert.True(result.IsSuccess);
    }

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

    [Fact]
    public async Task RemoveRole_UserDoesNotHaveRole_ReturnsFailure()
    {
        SetupCallerAsAdmin();
        var target = SetupTargetAsUser();
        _roleManager.FindByNameAsync("User").Returns(new ApplicationRole { Name = "User" });
        _userManager.IsInRoleAsync(target, "User").Returns(false);

        var result = await _sut.RemoveRoleAsync(_callerId, _targetId, "User");

        Assert.True(result.IsFailure);
        Assert.Contains("does not have", result.Error);
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
    public async Task LockUser_Valid_RevokesRefreshTokens()
    {
        SetupCallerAsAdmin();
        var target = SetupTargetAsUser();
        _userManager.SetLockoutEndDateAsync(target, Arg.Any<DateTimeOffset>())
            .Returns(IdentityResult.Success);

        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "hashed",
            UserId = _targetId,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
            ExpiredAt = _timeProvider.GetUtcNow().UtcDateTime.AddDays(7),
            IsUsed = false,
            IsInvalidated = false
        });
        await _dbContext.SaveChangesAsync();

        await _sut.LockUserAsync(_callerId, _targetId);

        var token = Assert.Single(_dbContext.RefreshTokens);
        Assert.True(token.IsInvalidated);
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

    [Fact]
    public async Task LockUser_InsufficientHierarchy_ReturnsFailure()
    {
        // Caller is User (rank 1), target is Admin (rank 2)
        var caller = new ApplicationUser { Id = _callerId, UserName = "user@test.com" };
        _userManager.FindByIdAsync(_callerId.ToString()).Returns(caller);
        _userManager.GetRolesAsync(caller).Returns(new List<string> { AppRoles.User });

        var target = new ApplicationUser { Id = _targetId, UserName = "admin@test.com" };
        _userManager.FindByIdAsync(_targetId.ToString()).Returns(target);
        _userManager.GetRolesAsync(target).Returns(new List<string> { AppRoles.Admin });

        var result = await _sut.LockUserAsync(_callerId, _targetId);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Admin.HierarchyInsufficient, result.Error);
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
    public async Task UnlockUser_Valid_ResetsAccessFailedCount()
    {
        SetupCallerAsAdmin();
        var target = SetupTargetAsUser();
        _userManager.SetLockoutEndDateAsync(target, null).Returns(IdentityResult.Success);
        _userManager.ResetAccessFailedCountAsync(target).Returns(IdentityResult.Success);

        await _sut.UnlockUserAsync(_callerId, _targetId);

        await _userManager.Received(1).ResetAccessFailedCountAsync(target);
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
    public async Task DeleteUser_Valid_ReturnsSuccess()
    {
        SetupCallerAsAdmin();
        var target = SetupTargetAsUser();
        _userManager.DeleteAsync(target).Returns(IdentityResult.Success);

        var result = await _sut.DeleteUserAsync(_callerId, _targetId);

        Assert.True(result.IsSuccess);
    }

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

    [Fact]
    public async Task DeleteUser_LastAdmin_ReturnsFailure()
    {
        // Caller must be SuperAdmin (rank 3) to pass hierarchy check against Admin target (rank 2)
        var caller = new ApplicationUser { Id = _callerId, UserName = "superadmin@test.com" };
        _userManager.FindByIdAsync(_callerId.ToString()).Returns(caller);
        _userManager.GetRolesAsync(caller).Returns(new List<string> { AppRoles.SuperAdmin });

        // Target is the only Admin
        var target = new ApplicationUser { Id = _targetId, UserName = "target@test.com" };
        _userManager.FindByIdAsync(_targetId.ToString()).Returns(target);
        _userManager.GetRolesAsync(target).Returns(new List<string> { AppRoles.Admin });

        // Simulate only 1 user in Admin role
        var adminRole = new ApplicationRole { Id = Guid.NewGuid(), Name = AppRoles.Admin };
        _roleManager.FindByNameAsync(AppRoles.Admin).Returns(adminRole);
        _dbContext.UserRoles.Add(new IdentityUserRole<Guid> { RoleId = adminRole.Id, UserId = _targetId });
        await _dbContext.SaveChangesAsync();

        var result = await _sut.DeleteUserAsync(_callerId, _targetId);

        Assert.True(result.IsFailure);
        Assert.Contains("last user", result.Error);
    }

    #endregion

    #region GetUserById

    [Fact]
    public async Task GetUserById_Found_ReturnsSuccess()
    {
        var user = new ApplicationUser
        {
            Id = _targetId,
            UserName = "user@test.com",
            Email = "user@test.com",
            FirstName = "John",
            LastName = "Doe"
        };
        _userManager.FindByIdAsync(_targetId.ToString()).Returns(user);
        _userManager.GetRolesAsync(user).Returns(new List<string> { AppRoles.User });

        var result = await _sut.GetUserByIdAsync(_targetId);

        Assert.True(result.IsSuccess);
        Assert.Equal("user@test.com", result.Value.UserName);
        Assert.Equal("John", result.Value.FirstName);
    }

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
