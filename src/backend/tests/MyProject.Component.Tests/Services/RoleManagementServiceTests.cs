using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Hybrid;
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
    private readonly HybridCache _hybridCache;
    private readonly IAuditService _auditService;
    private readonly MyProjectDbContext _dbContext;
    private readonly RoleManagementService _sut;

    public RoleManagementServiceTests()
    {
        _roleManager = IdentityMockHelpers.CreateMockRoleManager();
        _userManager = IdentityMockHelpers.CreateMockUserManager();
        _hybridCache = Substitute.For<HybridCache>();
        _dbContext = TestDbContextFactory.Create();
        _auditService = Substitute.For<IAuditService>();
        var logger = Substitute.For<ILogger<RoleManagementService>>();

        _sut = new RoleManagementService(
            _roleManager, _userManager, _dbContext, _hybridCache, _auditService,
            new PermissionEscalationGuard(_dbContext), logger);
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
    [InlineData("Superuser")]
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
        var role = TestRoles.Create(AppRoles.Admin, roleId);
        role.Description = "Old";
        _roleManager.FindByIdAsync(roleId.ToString()).Returns(role);
        _roleManager.UpdateAsync(role).Returns(IdentityResult.Success);

        var result = await _sut.UpdateRoleAsync(roleId, new UpdateRoleInput(null, "New description"));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task UpdateRole_SystemRoleRename_ReturnsFailure()
    {
        var roleId = Guid.NewGuid();
        var role = TestRoles.Create(AppRoles.Admin, roleId);
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
        var role = TestRoles.Create(AppRoles.Admin, roleId);
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
        TestRoles.SeedAssigned(_dbContext, callerId, AppRoles.Admin,
            AppPermissions.Users.View, AppPermissions.Users.Manage);

        var result = await _sut.SetRolePermissionsAsync(roleId,
            new SetRolePermissionsInput([AppPermissions.Users.View, AppPermissions.Users.Manage]),
            callerId);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task SetPermissions_SuperuserRole_ReturnsFailure()
    {
        var roleId = Guid.NewGuid();
        var role = TestRoles.Create(AppRoles.Superuser, roleId);
        _roleManager.FindByIdAsync(roleId.ToString()).Returns(role);

        var result = await _sut.SetRolePermissionsAsync(roleId,
            new SetRolePermissionsInput([AppPermissions.Users.View]),
            Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Roles.SuperuserPermissionsFixed, result.Error);
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
        TestRoles.SeedAssigned(_dbContext, callerId, AppRoles.Admin, AppPermissions.Roles.Manage);

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
        TestRoles.SeedAssigned(_dbContext, callerId, AppRoles.Admin,
            AppPermissions.Users.View, AppPermissions.Users.Manage);

        var result = await _sut.SetRolePermissionsAsync(roleId,
            new SetRolePermissionsInput([AppPermissions.Users.View, AppPermissions.Users.Manage, AppPermissions.Roles.View]),
            callerId);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Roles.CannotGrantUnheldPermission, result.Error);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task SetPermissions_CallerWithoutRoles_ReturnsForbidden()
    {
        var roleId = Guid.NewGuid();
        var role = new ApplicationRole { Id = roleId, Name = "CustomRole" };
        _roleManager.FindByIdAsync(roleId.ToString()).Returns(role);

        // Caller has no role assignments in the database, so they hold no permissions.
        var callerId = Guid.NewGuid();

        var result = await _sut.SetRolePermissionsAsync(roleId,
            new SetRolePermissionsInput([AppPermissions.Users.View]),
            callerId);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Roles.CannotGrantUnheldPermission, result.Error);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task SetPermissions_EmptyPermissions_SkipsEscalationCheck()
    {
        var roleId = Guid.NewGuid();
        var role = new ApplicationRole { Id = roleId, Name = "CustomRole" };
        _roleManager.FindByIdAsync(roleId.ToString()).Returns(role);

        // Caller has no seeded roles or permissions - an empty list short-circuits the guard.
        // The method will fail at ExecuteDeleteAsync (InMemory limitation), confirming the
        // escalation guard passed without requiring any caller permissions.
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.SetRolePermissionsAsync(roleId,
            new SetRolePermissionsInput([]),
            Guid.NewGuid()));
    }

    [Fact]
    public async Task SetPermissions_MultipleCallerRoles_AggregatesPermissions()
    {
        var roleId = Guid.NewGuid();
        var role = new ApplicationRole { Id = roleId, Name = "CustomRole" };
        _roleManager.FindByIdAsync(roleId.ToString()).Returns(role);

        var callerId = Guid.NewGuid();
        TestRoles.SeedAssigned(_dbContext, callerId, "RoleA", AppPermissions.Users.View);
        TestRoles.SeedAssigned(_dbContext, callerId, "RoleB", AppPermissions.Users.Manage);

        // Caller holds users.view via RoleA and users.manage via RoleB - combined they cover both.
        // The method will fail at ExecuteDeleteAsync (InMemory limitation), confirming the guard passed.
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.SetRolePermissionsAsync(roleId,
            new SetRolePermissionsInput([AppPermissions.Users.View, AppPermissions.Users.Manage]),
            callerId));
    }

    [Fact]
    public async Task SetPermissions_OrphanRoleAssignment_ContinuesGracefully()
    {
        var roleId = Guid.NewGuid();
        var role = new ApplicationRole { Id = roleId, Name = "CustomRole" };
        _roleManager.FindByIdAsync(roleId.ToString()).Returns(role);

        var callerId = Guid.NewGuid();

        // Orphan assignment: the referenced role row does not exist - skipped by the join.
        _dbContext.UserRoles.Add(new IdentityUserRole<Guid> { UserId = callerId, RoleId = Guid.NewGuid() });
        await _dbContext.SaveChangesAsync();

        // RealRole has the required permission
        TestRoles.SeedAssigned(_dbContext, callerId, "RealRole", AppPermissions.Users.View);

        // Should pass the escalation guard despite the orphan assignment
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.SetRolePermissionsAsync(roleId,
            new SetRolePermissionsInput([AppPermissions.Users.View]),
            callerId));
    }

    [Fact]
    public async Task SetPermissions_GrantsAllCaller_SkipsPermissionCheck()
    {
        var roleId = Guid.NewGuid();
        var role = new ApplicationRole { Id = roleId, Name = "CustomRole" };
        _roleManager.FindByIdAsync(roleId.ToString()).Returns(role);

        var callerId = Guid.NewGuid();
        TestRoles.SeedAssigned(_dbContext, callerId, AppRoles.Superuser);

        // A grants-all caller passes the escalation check even without explicit permission
        // claims. The method will fail at ExecuteDeleteAsync (InMemory provider limitation),
        // confirming the escalation guard passed.
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.SetRolePermissionsAsync(roleId,
            new SetRolePermissionsInput([AppPermissions.Users.View, AppPermissions.Users.Manage]),
            callerId));

        // The guard resolves held permissions from the database, never via RoleManager claims
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

}
