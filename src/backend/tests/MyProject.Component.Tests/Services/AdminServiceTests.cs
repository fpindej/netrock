using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using MyProject.Application.Caching;
using MyProject.Application.Features.Admin.Dtos;
using MyProject.Application.Features.Audit;
using MyProject.Application.Features.Email;
using MyProject.Application.Identity.Constants;
using MyProject.Component.Tests.Fixtures;
using MyProject.Infrastructure.Features.Admin.Services;
using MyProject.Infrastructure.Features.Authentication.Models;
using MyProject.Infrastructure.Features.Authentication.Options;
using MyProject.Infrastructure.Features.Authentication.Services;
using MyProject.Infrastructure.Features.Email.Options;
using MyProject.Infrastructure.Persistence;
using MyProject.Shared;

namespace MyProject.Component.Tests.Services;

public class AdminServiceTests : IDisposable
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ICacheService _cacheService;
    private readonly IEmailService _emailService;
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
        _emailService = Substitute.For<IEmailService>();
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2025, 1, 15, 12, 0, 0, TimeSpan.Zero));
        _dbContext = TestDbContextFactory.Create();
        var logger = Substitute.For<ILogger<AdminService>>();
        var authOptions = Options.Create(new AuthenticationOptions
        {
            Jwt = new AuthenticationOptions.JwtOptions
            {
                Key = "ThisIsATestSigningKeyWithAtLeast32Chars!",
                Issuer = "test-issuer",
                Audience = "test-audience"
            }
        });
        var emailOptions = Options.Create(new EmailOptions { FrontendBaseUrl = "https://example.com" });
        var emailTokenService = new EmailTokenService(_dbContext, _timeProvider, authOptions);

        _sut = new AdminService(
            _userManager, _roleManager, _dbContext, _cacheService, _timeProvider,
            _emailService, emailTokenService, Substitute.For<IAuditService>(), emailOptions, logger);
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

    private ApplicationUser SetupTargetAsUser(bool emailConfirmed = true)
    {
        var target = new ApplicationUser { Id = _targetId, UserName = "user@test.com", EmailConfirmed = emailConfirmed };
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

    [Fact]
    public async Task AssignRole_SystemRole_UnverifiedEmail_ReturnsFailure()
    {
        SetupCallerAsAdmin();
        var target = new ApplicationUser { Id = _targetId, UserName = "user@test.com", EmailConfirmed = false };
        _userManager.FindByIdAsync(_targetId.ToString()).Returns(target);
        _userManager.GetRolesAsync(target).Returns(new List<string>());
        _roleManager.FindByNameAsync("User").Returns(new ApplicationRole { Name = "User" });
        _userManager.IsInRoleAsync(target, "User").Returns(false);

        var result = await _sut.AssignRoleAsync(_callerId, _targetId, new AssignRoleInput("User"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Admin.EmailVerificationRequired, result.Error);
    }

    [Fact]
    public async Task AssignRole_CustomRole_UnverifiedEmail_Succeeds()
    {
        SetupCallerAsAdmin();
        var target = new ApplicationUser { Id = _targetId, UserName = "user@test.com", EmailConfirmed = false };
        _userManager.FindByIdAsync(_targetId.ToString()).Returns(target);
        _userManager.GetRolesAsync(target).Returns(new List<string>());
        _roleManager.FindByNameAsync("CustomRole").Returns(new ApplicationRole { Name = "CustomRole" });
        _userManager.IsInRoleAsync(target, "CustomRole").Returns(false);
        _userManager.AddToRoleAsync(target, "CustomRole").Returns(IdentityResult.Success);

        var result = await _sut.AssignRoleAsync(_callerId, _targetId, new AssignRoleInput("CustomRole"));

        Assert.True(result.IsSuccess);
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

    #region GetUsersAsync

    [Fact(Skip = "EF InMemory provider does not support GroupBy — requires Testcontainers (issue #174)")]
    public async Task GetUsers_ReturnsPagedResults()
    {
        // Seed users into InMemory database
        for (var i = 0; i < 5; i++)
        {
            _dbContext.Users.Add(new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = $"user{i}@test.com",
                Email = $"user{i}@test.com",
                NormalizedUserName = $"USER{i}@TEST.COM"
            });
        }
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetUsersAsync(1, 3);

        Assert.Equal(5, result.TotalCount);
        Assert.Equal(3, result.Users.Count);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(3, result.PageSize);
    }

    [Fact(Skip = "EF InMemory provider does not support GroupBy — requires Testcontainers (issue #174)")]
    public async Task GetUsers_WithSearch_FiltersResults()
    {
        _dbContext.Users.Add(new ApplicationUser
        {
            Id = Guid.NewGuid(), UserName = "alice@test.com",
            NormalizedUserName = "ALICE@TEST.COM", FirstName = "Alice"
        });
        _dbContext.Users.Add(new ApplicationUser
        {
            Id = Guid.NewGuid(), UserName = "bob@test.com",
            NormalizedUserName = "BOB@TEST.COM", FirstName = "Bob"
        });
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetUsersAsync(1, 10, "alice");

        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Users);
        Assert.Equal("alice@test.com", result.Users[0].UserName);
    }

    [Fact(Skip = "EF InMemory provider does not support GroupBy — requires Testcontainers (issue #174)")]
    public async Task GetUsers_MapsRolesCorrectly()
    {
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        _dbContext.Users.Add(new ApplicationUser
        {
            Id = userId, UserName = "withRole@test.com",
            NormalizedUserName = "WITHROLE@TEST.COM"
        });
        _dbContext.Roles.Add(new ApplicationRole { Id = roleId, Name = "Admin", NormalizedName = "ADMIN" });
        _dbContext.UserRoles.Add(new IdentityUserRole<Guid> { UserId = userId, RoleId = roleId });
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetUsersAsync(1, 10);

        var user = Assert.Single(result.Users);
        Assert.Contains("Admin", user.Roles);
    }

    [Fact(Skip = "EF InMemory provider does not support GroupBy — requires Testcontainers (issue #174)")]
    public async Task GetUsers_CalculatesLockoutStatus()
    {
        _dbContext.Users.Add(new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "locked@test.com",
            NormalizedUserName = "LOCKED@TEST.COM",
            LockoutEnd = _timeProvider.GetUtcNow().AddYears(100)
        });
        _dbContext.Users.Add(new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "unlocked@test.com",
            NormalizedUserName = "UNLOCKED@TEST.COM",
            LockoutEnd = null
        });
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetUsersAsync(1, 10);

        Assert.Equal(2, result.Users.Count);
        var locked = result.Users.First(u => u.UserName == "locked@test.com");
        var unlocked = result.Users.First(u => u.UserName == "unlocked@test.com");
        Assert.True(locked.IsLockedOut);
        Assert.False(unlocked.IsLockedOut);
    }

    #endregion

    #region GetRolesAsync

    [Fact]
    public async Task GetRoles_ReturnsAllRoles()
    {
        var role1 = new ApplicationRole { Id = Guid.NewGuid(), Name = AppRoles.Admin, NormalizedName = "ADMIN" };
        var role2 = new ApplicationRole { Id = Guid.NewGuid(), Name = "Custom", NormalizedName = "CUSTOM" };

        // Use the InMemory DbContext roles so roleManager.Roles resolves via EF
        _dbContext.Roles.Add(role1);
        _dbContext.Roles.Add(role2);
        await _dbContext.SaveChangesAsync();
        _roleManager.Roles.Returns(_dbContext.Roles);

        var result = await _sut.GetRolesAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetRoles_IdentifiesSystemRoles()
    {
        var adminRole = new ApplicationRole { Id = Guid.NewGuid(), Name = AppRoles.Admin, NormalizedName = "ADMIN" };
        var customRole = new ApplicationRole { Id = Guid.NewGuid(), Name = "Custom", NormalizedName = "CUSTOM" };

        _dbContext.Roles.Add(adminRole);
        _dbContext.Roles.Add(customRole);
        await _dbContext.SaveChangesAsync();
        _roleManager.Roles.Returns(_dbContext.Roles);

        var result = await _sut.GetRolesAsync();

        var admin = result.First(r => r.Name == AppRoles.Admin);
        var custom = result.First(r => r.Name == "Custom");
        Assert.True(admin.IsSystem);
        Assert.False(custom.IsSystem);
    }

    [Fact]
    public async Task GetRoles_CountsUsersPerRole()
    {
        var roleId = Guid.NewGuid();
        _dbContext.Roles.Add(new ApplicationRole { Id = roleId, Name = "TestRole", NormalizedName = "TESTROLE" });
        _dbContext.UserRoles.Add(new IdentityUserRole<Guid> { RoleId = roleId, UserId = Guid.NewGuid() });
        _dbContext.UserRoles.Add(new IdentityUserRole<Guid> { RoleId = roleId, UserId = Guid.NewGuid() });
        await _dbContext.SaveChangesAsync();
        _roleManager.Roles.Returns(_dbContext.Roles);

        var result = await _sut.GetRolesAsync();

        var role = Assert.Single(result);
        Assert.Equal(2, role.UserCount);
    }

    #endregion

    #region VerifyEmail

    [Fact]
    public async Task VerifyEmail_Valid_ReturnsSuccess()
    {
        SetupCallerAsAdmin();
        var target = SetupTargetAsUser();
        target.EmailConfirmed = false;
        _userManager.GenerateEmailConfirmationTokenAsync(target).Returns("token");
        _userManager.ConfirmEmailAsync(target, "token").Returns(IdentityResult.Success);

        var result = await _sut.VerifyEmailAsync(_callerId, _targetId);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task VerifyEmail_UserNotFound_ReturnsNotFound()
    {
        _userManager.FindByIdAsync(_targetId.ToString()).Returns((ApplicationUser?)null);

        var result = await _sut.VerifyEmailAsync(_callerId, _targetId);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Admin.UserNotFound, result.Error);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task VerifyEmail_AlreadyVerified_ReturnsFailure()
    {
        SetupCallerAsAdmin();
        var target = SetupTargetAsUser();
        target.EmailConfirmed = true;

        var result = await _sut.VerifyEmailAsync(_callerId, _targetId);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Auth.EmailAlreadyVerified, result.Error);
    }

    [Fact]
    public async Task VerifyEmail_InsufficientHierarchy_ReturnsFailure()
    {
        // Caller is User (rank 1), target is Admin (rank 2)
        var caller = new ApplicationUser { Id = _callerId, UserName = "user@test.com" };
        _userManager.FindByIdAsync(_callerId.ToString()).Returns(caller);
        _userManager.GetRolesAsync(caller).Returns(new List<string> { AppRoles.User });

        var target = new ApplicationUser { Id = _targetId, UserName = "admin@test.com", EmailConfirmed = false };
        _userManager.FindByIdAsync(_targetId.ToString()).Returns(target);
        _userManager.GetRolesAsync(target).Returns(new List<string> { AppRoles.Admin });

        var result = await _sut.VerifyEmailAsync(_callerId, _targetId);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Admin.HierarchyInsufficient, result.Error);
    }

    #endregion

    #region SendPasswordReset

    [Fact]
    public async Task SendPasswordReset_Valid_ReturnsSuccess()
    {
        SetupCallerAsAdmin();
        var target = SetupTargetAsUser();
        target.Email = "user@test.com";
        _userManager.GeneratePasswordResetTokenAsync(target).Returns("reset-token");

        var result = await _sut.SendPasswordResetAsync(_callerId, _targetId);

        Assert.True(result.IsSuccess);
        await _emailService.Received(1).SendEmailAsync(
            Arg.Is<EmailMessage>(m =>
                m.To == "user@test.com" &&
                !m.PlainTextBody!.Contains("email=", StringComparison.OrdinalIgnoreCase)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendPasswordReset_CreatesOpaqueEmailToken()
    {
        SetupCallerAsAdmin();
        var target = SetupTargetAsUser();
        target.Email = "user@test.com";
        _userManager.GeneratePasswordResetTokenAsync(target).Returns("reset-token");

        await _sut.SendPasswordResetAsync(_callerId, _targetId);

        var emailToken = Assert.Single(_dbContext.EmailTokens);
        Assert.Equal(_targetId, emailToken.UserId);
        Assert.Equal(EmailTokenPurpose.PasswordReset, emailToken.Purpose);
        Assert.Equal("reset-token", emailToken.IdentityToken);
        Assert.False(emailToken.IsUsed);
    }

    [Fact]
    public async Task SendPasswordReset_UserNotFound_ReturnsNotFound()
    {
        _userManager.FindByIdAsync(_targetId.ToString()).Returns((ApplicationUser?)null);

        var result = await _sut.SendPasswordResetAsync(_callerId, _targetId);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Admin.UserNotFound, result.Error);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task SendPasswordReset_InsufficientHierarchy_ReturnsFailure()
    {
        var caller = new ApplicationUser { Id = _callerId, UserName = "user@test.com" };
        _userManager.FindByIdAsync(_callerId.ToString()).Returns(caller);
        _userManager.GetRolesAsync(caller).Returns(new List<string> { AppRoles.User });

        var target = new ApplicationUser { Id = _targetId, UserName = "admin@test.com" };
        _userManager.FindByIdAsync(_targetId.ToString()).Returns(target);
        _userManager.GetRolesAsync(target).Returns(new List<string> { AppRoles.Admin });

        var result = await _sut.SendPasswordResetAsync(_callerId, _targetId);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Admin.HierarchyInsufficient, result.Error);
    }

    #endregion

    #region CreateUser

    [Fact]
    public async Task CreateUser_Valid_ReturnsUserId()
    {
        var newUserId = Guid.NewGuid();
        _userManager.FindByEmailAsync("new@test.com").Returns((ApplicationUser?)null);
        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
            .Returns(callInfo =>
            {
                callInfo.Arg<ApplicationUser>().Id = newUserId;
                return IdentityResult.Success;
            });
        _userManager.AddToRoleAsync(Arg.Any<ApplicationUser>(), AppRoles.User)
            .Returns(IdentityResult.Success);
        _userManager.GeneratePasswordResetTokenAsync(Arg.Any<ApplicationUser>())
            .Returns("reset-token");

        var result = await _sut.CreateUserAsync(_callerId, new CreateUserInput("new@test.com", "John", "Doe"));

        Assert.True(result.IsSuccess);
        Assert.Equal(newUserId, result.Value);
        await _emailService.Received(1).SendEmailAsync(
            Arg.Is<EmailMessage>(m => m.To == "new@test.com"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateUser_DuplicateEmail_ReturnsFailure()
    {
        _userManager.FindByEmailAsync("existing@test.com")
            .Returns(new ApplicationUser { Id = Guid.NewGuid(), Email = "existing@test.com" });

        var result = await _sut.CreateUserAsync(_callerId, new CreateUserInput("existing@test.com", null, null));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Admin.EmailAlreadyRegistered, result.Error);
    }

    [Fact]
    public async Task CreateUser_SendsInvitationEmail_WithOpaqueToken()
    {
        _userManager.FindByEmailAsync("invite@test.com").Returns((ApplicationUser?)null);
        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
            .Returns(IdentityResult.Success);
        _userManager.AddToRoleAsync(Arg.Any<ApplicationUser>(), AppRoles.User)
            .Returns(IdentityResult.Success);
        _userManager.GeneratePasswordResetTokenAsync(Arg.Any<ApplicationUser>())
            .Returns("reset-token");

        await _sut.CreateUserAsync(_callerId, new CreateUserInput("invite@test.com", null, null));

        await _emailService.Received(1).SendEmailAsync(
            Arg.Is<EmailMessage>(m =>
                m.To == "invite@test.com" &&
                m.Subject == "You've Been Invited" &&
                !m.PlainTextBody!.Contains("email=", StringComparison.OrdinalIgnoreCase)),
            Arg.Any<CancellationToken>());

        var emailToken = Assert.Single(_dbContext.EmailTokens);
        Assert.Equal(EmailTokenPurpose.PasswordReset, emailToken.Purpose);
        Assert.Equal("reset-token", emailToken.IdentityToken);
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
