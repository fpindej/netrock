using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using MyProject.Application.Caching;
using MyProject.Application.Features.Admin.Dtos;
using MyProject.Application.Features.Audit;
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
    private readonly IAuditService _auditService;
    private readonly MyProjectDbContext _dbContext;
    private readonly RoleManagementService _sut;

    public RoleManagementServiceTests()
    {
        _roleManager = IdentityMockHelpers.CreateMockRoleManager();
        _userManager = IdentityMockHelpers.CreateMockUserManager();
        _cacheService = Substitute.For<ICacheService>();
        _dbContext = TestDbContextFactory.Create();
        _auditService = Substitute.For<IAuditService>();
        var logger = Substitute.For<ILogger<RoleManagementService>>();

        _sut = new RoleManagementService(
            _roleManager, _userManager, _dbContext, _cacheService, _auditService, logger);
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
        await _auditService.Received(1).LogAsync(
            AuditActions.AdminCreateRole,
            userId: Arg.Any<Guid?>(),
            targetEntityType: "Role",
            targetEntityId: Arg.Any<Guid?>(),
            metadata: Arg.Is<string>(m => m.Contains("CustomRole")),
            ct: Arg.Any<CancellationToken>());
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
        await _auditService.Received(1).LogAsync(
            AuditActions.AdminUpdateRole,
            userId: Arg.Any<Guid?>(),
            targetEntityType: "Role",
            targetEntityId: roleId,
            metadata: Arg.Any<string?>(),
            ct: Arg.Any<CancellationToken>());
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
        await _auditService.Received(1).LogAsync(
            AuditActions.AdminDeleteRole,
            userId: Arg.Any<Guid?>(),
            targetEntityType: "Role",
            targetEntityId: roleId,
            metadata: Arg.Any<string?>(),
            ct: Arg.Any<CancellationToken>());
    }

    #endregion

    #region SetRolePermissions

    [Fact(Skip = "InMemory EF provider does not support ExecuteDeleteAsync — requires Testcontainers (issue #174)")]
    public async Task SetPermissions_ValidPermissions_ReturnsSuccess()
    {
        var roleId = Guid.NewGuid();
        var role = new ApplicationRole { Id = roleId, Name = "CustomRole" };
        _roleManager.FindByIdAsync(roleId.ToString()).Returns(role);

        var callerId = Guid.NewGuid();
        MockCallerWithPermissions(callerId, "Admin",
            [AppPermissions.Users.View, AppPermissions.Users.Manage]);

        var result = await _sut.SetRolePermissionsAsync(roleId,
            new SetRolePermissionsInput([AppPermissions.Users.View, AppPermissions.Users.Manage]),
            callerId);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task SetPermissions_SuperAdminRole_ReturnsFailure()
    {
        var roleId = Guid.NewGuid();
        var role = new ApplicationRole { Id = roleId, Name = AppRoles.SuperAdmin };
        _roleManager.FindByIdAsync(roleId.ToString()).Returns(role);

        var result = await _sut.SetRolePermissionsAsync(roleId,
            new SetRolePermissionsInput([AppPermissions.Users.View]),
            Guid.NewGuid());

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
            new SetRolePermissionsInput(["invalid.permission"]),
            Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Roles.InvalidPermission, result.Error);
    }

    [Fact]
    public async Task SetPermissions_NotFound_ReturnsNotFound()
    {
        _roleManager.FindByIdAsync(Arg.Any<string>()).Returns((ApplicationRole?)null);

        var result = await _sut.SetRolePermissionsAsync(Guid.NewGuid(),
            new SetRolePermissionsInput([AppPermissions.Users.View]),
            Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Roles.RoleNotFound, result.Error);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task SetPermissions_CallerLacksPermission_ReturnsForbidden()
    {
        var roleId = Guid.NewGuid();
        var role = new ApplicationRole { Id = roleId, Name = "CustomRole" };
        _roleManager.FindByIdAsync(roleId.ToString()).Returns(role);

        var callerId = Guid.NewGuid();
        MockCallerWithPermissions(callerId, "Admin", [AppPermissions.Roles.Manage]);

        var result = await _sut.SetRolePermissionsAsync(roleId,
            new SetRolePermissionsInput([AppPermissions.Users.View]),
            callerId);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Roles.CannotGrantUnheldPermission, result.Error);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task SetPermissions_CallerHoldsSubsetOfRequested_ReturnsForbidden()
    {
        var roleId = Guid.NewGuid();
        var role = new ApplicationRole { Id = roleId, Name = "CustomRole" };
        _roleManager.FindByIdAsync(roleId.ToString()).Returns(role);

        var callerId = Guid.NewGuid();
        MockCallerWithPermissions(callerId, "Admin",
            [AppPermissions.Users.View, AppPermissions.Users.Manage]);

        var result = await _sut.SetRolePermissionsAsync(roleId,
            new SetRolePermissionsInput([AppPermissions.Users.View, AppPermissions.Users.Manage, AppPermissions.Roles.View]),
            callerId);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Roles.CannotGrantUnheldPermission, result.Error);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task SetPermissions_CallerNotFound_ReturnsForbidden()
    {
        var roleId = Guid.NewGuid();
        var role = new ApplicationRole { Id = roleId, Name = "CustomRole" };
        _roleManager.FindByIdAsync(roleId.ToString()).Returns(role);

        var callerId = Guid.NewGuid();
        _userManager.FindByIdAsync(callerId.ToString()).Returns((ApplicationUser?)null);

        var result = await _sut.SetRolePermissionsAsync(roleId,
            new SetRolePermissionsInput([AppPermissions.Users.View]),
            callerId);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Auth.InsufficientPermissions, result.Error);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task SetPermissions_EmptyPermissions_SkipsEscalationCheck()
    {
        var roleId = Guid.NewGuid();
        var role = new ApplicationRole { Id = roleId, Name = "CustomRole" };
        _roleManager.FindByIdAsync(roleId.ToString()).Returns(role);

        // Caller does not need to be mocked — empty list short-circuits before lookup.
        // The method will fail at ExecuteDeleteAsync (InMemory limitation), confirming the
        // escalation guard passed without requiring any caller permissions.
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.SetRolePermissionsAsync(roleId,
            new SetRolePermissionsInput([]),
            Guid.NewGuid()));

        // Verify no caller lookup was attempted
        await _userManager.DidNotReceive().FindByIdAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task SetPermissions_MultipleCallerRoles_AggregatesPermissions()
    {
        var roleId = Guid.NewGuid();
        var role = new ApplicationRole { Id = roleId, Name = "CustomRole" };
        _roleManager.FindByIdAsync(roleId.ToString()).Returns(role);

        var callerId = Guid.NewGuid();
        MockCallerWithMultipleRoles(callerId,
            ("RoleA", [AppPermissions.Users.View]),
            ("RoleB", [AppPermissions.Users.Manage]));

        // Caller holds users.view via RoleA and users.manage via RoleB — combined they cover both.
        // The method will fail at ExecuteDeleteAsync (InMemory limitation), confirming the guard passed.
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.SetRolePermissionsAsync(roleId,
            new SetRolePermissionsInput([AppPermissions.Users.View, AppPermissions.Users.Manage]),
            callerId));

        // Verify both roles' claims were checked
        await _roleManager.Received(2).GetClaimsAsync(Arg.Any<ApplicationRole>());
    }

    [Fact]
    public async Task SetPermissions_CallerRoleNotFoundDuringAggregation_ContinuesGracefully()
    {
        var roleId = Guid.NewGuid();
        var role = new ApplicationRole { Id = roleId, Name = "CustomRole" };
        _roleManager.FindByIdAsync(roleId.ToString()).Returns(role);

        var callerId = Guid.NewGuid();
        var caller = new ApplicationUser { Id = callerId, UserName = "caller@test.com" };
        _userManager.FindByIdAsync(callerId.ToString()).Returns(caller);
        _userManager.GetRolesAsync(caller).Returns(["GhostRole", "RealRole"]);

        // GhostRole not found — should be skipped silently
        _roleManager.FindByNameAsync("GhostRole").Returns((ApplicationRole?)null);

        // RealRole has the required permission
        var realRole = new ApplicationRole { Id = Guid.NewGuid(), Name = "RealRole" };
        _roleManager.FindByNameAsync("RealRole").Returns(realRole);
        _roleManager.GetClaimsAsync(realRole).Returns(
            [new System.Security.Claims.Claim(AppPermissions.ClaimType, AppPermissions.Users.View)]);

        // Should pass escalation guard despite GhostRole being null
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.SetRolePermissionsAsync(roleId,
            new SetRolePermissionsInput([AppPermissions.Users.View]),
            callerId));

        // GhostRole was looked up but GetClaimsAsync was never called for it
        await _roleManager.Received(1).FindByNameAsync("GhostRole");
        await _roleManager.Received(1).GetClaimsAsync(realRole);
    }

    [Fact]
    public async Task SetPermissions_SuperAdminCaller_SkipsPermissionCheck()
    {
        var roleId = Guid.NewGuid();
        var role = new ApplicationRole { Id = roleId, Name = "CustomRole" };
        _roleManager.FindByIdAsync(roleId.ToString()).Returns(role);

        var callerId = Guid.NewGuid();
        MockCallerWithRole(callerId, AppRoles.SuperAdmin);

        // SuperAdmin caller should pass escalation check even though they don't
        // have explicit permission claims. The method will fail at ExecuteDeleteAsync
        // (InMemory provider limitation), confirming the escalation guard passed.
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.SetRolePermissionsAsync(roleId,
            new SetRolePermissionsInput([AppPermissions.Users.View, AppPermissions.Users.Manage]),
            callerId));

        // Verify the caller lookup was performed but no permission claims were checked
        await _userManager.Received(1).FindByIdAsync(callerId.ToString());
        await _userManager.Received(1).GetRolesAsync(Arg.Any<ApplicationUser>());
        await _roleManager.DidNotReceive().GetClaimsAsync(Arg.Any<ApplicationRole>());
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

    #region Helpers

    /// <summary>
    /// Mocks the UserManager/RoleManager chain so the caller is resolved with the given role
    /// and that role has the specified permission claims.
    /// </summary>
    private void MockCallerWithPermissions(Guid callerId, string roleName, string[] permissions)
    {
        var caller = new ApplicationUser { Id = callerId, UserName = "caller@test.com" };
        _userManager.FindByIdAsync(callerId.ToString()).Returns(caller);
        _userManager.GetRolesAsync(caller).Returns([roleName]);

        var callerRole = new ApplicationRole { Id = Guid.NewGuid(), Name = roleName };
        _roleManager.FindByNameAsync(roleName).Returns(callerRole);
        _roleManager.GetClaimsAsync(callerRole).Returns(
            permissions.Select(p => new System.Security.Claims.Claim(AppPermissions.ClaimType, p)).ToList());
    }

    /// <summary>
    /// Mocks the UserManager chain so the caller is resolved with the given role (no permission claims).
    /// </summary>
    private void MockCallerWithRole(Guid callerId, string roleName)
    {
        var caller = new ApplicationUser { Id = callerId, UserName = "caller@test.com" };
        _userManager.FindByIdAsync(callerId.ToString()).Returns(caller);
        _userManager.GetRolesAsync(caller).Returns([roleName]);
    }

    /// <summary>
    /// Mocks the UserManager/RoleManager chain so the caller has multiple roles, each with distinct permissions.
    /// </summary>
    private void MockCallerWithMultipleRoles(Guid callerId,
        params (string RoleName, string[] Permissions)[] roles)
    {
        var caller = new ApplicationUser { Id = callerId, UserName = "caller@test.com" };
        _userManager.FindByIdAsync(callerId.ToString()).Returns(caller);
        _userManager.GetRolesAsync(caller).Returns(roles.Select(r => r.RoleName).ToList());

        foreach (var (roleName, permissions) in roles)
        {
            var appRole = new ApplicationRole { Id = Guid.NewGuid(), Name = roleName };
            _roleManager.FindByNameAsync(roleName).Returns(appRole);
            _roleManager.GetClaimsAsync(appRole).Returns(
                permissions.Select(p => new System.Security.Claims.Claim(AppPermissions.ClaimType, p)).ToList());
        }
    }

    #endregion
}
