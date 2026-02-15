using Microsoft.AspNetCore.Identity;
using MyProject.Application.Caching;
using MyProject.Application.Caching.Constants;
using MyProject.Application.Cookies;
using MyProject.Application.Cookies.Constants;
using MyProject.Application.Features.Authentication.Dtos;
using MyProject.Application.Identity;
using MyProject.Application.Identity.Constants;
using MyProject.Application.Identity.Dtos;
using MyProject.Component.Tests.Fixtures;
using MyProject.Infrastructure.Features.Authentication.Models;
using MyProject.Infrastructure.Identity.Services;
using MyProject.Infrastructure.Persistence;
using MyProject.Shared;

namespace MyProject.Component.Tests.Services;

public class UserServiceTests : IDisposable
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IUserContext _userContext;
    private readonly ICacheService _cacheService;
    private readonly ICookieService _cookieService;
    private readonly MyProjectDbContext _dbContext;
    private readonly UserService _sut;

    private readonly Guid _userId = Guid.NewGuid();

    public UserServiceTests()
    {
        _userManager = IdentityMockHelpers.CreateMockUserManager();
        _roleManager = IdentityMockHelpers.CreateMockRoleManager();
        _userContext = Substitute.For<IUserContext>();
        _cacheService = Substitute.For<ICacheService>();
        _cookieService = Substitute.For<ICookieService>();
        _dbContext = TestDbContextFactory.Create();

        _sut = new UserService(
            _userManager, _roleManager, _userContext, _cacheService, _dbContext, _cookieService);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _userManager.Dispose();
    }

    #region GetCurrentUser

    [Fact]
    public async Task GetCurrentUser_Authenticated_ReturnsUserData()
    {
        var user = new ApplicationUser
        {
            Id = _userId,
            UserName = "test@example.com",
            FirstName = "John",
            LastName = "Doe"
        };
        _userContext.UserId.Returns(_userId);
        _userManager.FindByIdAsync(_userId.ToString()).Returns(user);
        _userManager.GetRolesAsync(user).Returns(new List<string> { "User" });

        var result = await _sut.GetCurrentUserAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal("test@example.com", result.Value.UserName);
        Assert.Equal("John", result.Value.FirstName);
    }

    [Fact]
    public async Task GetCurrentUser_NotAuthenticated_ReturnsUnauthorized()
    {
        _userContext.UserId.Returns((Guid?)null);

        var result = await _sut.GetCurrentUserAsync();

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.User.NotAuthenticated, result.Error);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task GetCurrentUser_UserNotFound_ReturnsFailure()
    {
        _userContext.UserId.Returns(_userId);
        _userManager.FindByIdAsync(_userId.ToString()).Returns((ApplicationUser?)null);

        var result = await _sut.GetCurrentUserAsync();

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.User.NotFound, result.Error);
    }

    #endregion

    #region UpdateProfile

    [Fact]
    public async Task UpdateProfile_Valid_ReturnsUpdatedUser()
    {
        var user = new ApplicationUser
        {
            Id = _userId,
            UserName = "test@example.com"
        };
        _userContext.UserId.Returns(_userId);
        _userManager.FindByIdAsync(_userId.ToString()).Returns(user);
        _userManager.UpdateAsync(user).Returns(IdentityResult.Success);
        _userManager.GetRolesAsync(user).Returns(new List<string> { "User" });

        var result = await _sut.UpdateProfileAsync(
            new UpdateProfileInput("Jane", "Doe", null, "Bio text", null));

        Assert.True(result.IsSuccess);
        Assert.Equal("Jane", result.Value.FirstName);
        Assert.Equal("Doe", result.Value.LastName);
    }

    [Fact]
    public async Task UpdateProfile_DuplicatePhone_ReturnsFailure()
    {
        _dbContext.Users.Add(new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "other@example.com",
            PhoneNumber = "+420123456789"
        });
        await _dbContext.SaveChangesAsync();

        _userContext.UserId.Returns(_userId);
        _userManager.FindByIdAsync(_userId.ToString())
            .Returns(new ApplicationUser { Id = _userId, UserName = "test@example.com" });
        _userManager.Users.Returns(_dbContext.Users);

        var result = await _sut.UpdateProfileAsync(
            new UpdateProfileInput(null, null, "+420123456789", null, null));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.User.PhoneNumberTaken, result.Error);
    }

    [Fact]
    public async Task UpdateProfile_NotAuthenticated_ReturnsUnauthorized()
    {
        _userContext.UserId.Returns((Guid?)null);

        var result = await _sut.UpdateProfileAsync(
            new UpdateProfileInput("Jane", null, null, null, null));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.User.NotAuthenticated, result.Error);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task UpdateProfile_UserNotFound_ReturnsFailure()
    {
        _userContext.UserId.Returns(_userId);
        _userManager.FindByIdAsync(_userId.ToString()).Returns((ApplicationUser?)null);

        var result = await _sut.UpdateProfileAsync(
            new UpdateProfileInput("Jane", null, null, null, null));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.User.NotFound, result.Error);
    }

    #endregion

    #region DeleteAccount

    [Fact]
    public async Task DeleteAccount_Valid_ReturnsSuccess()
    {
        var user = new ApplicationUser { Id = _userId, UserName = "test@example.com" };
        _userContext.UserId.Returns(_userId);
        _userManager.FindByIdAsync(_userId.ToString()).Returns(user);
        _userManager.CheckPasswordAsync(user, "correct").Returns(true);
        _userManager.GetRolesAsync(user).Returns(new List<string> { "User" });
        _userManager.DeleteAsync(user).Returns(IdentityResult.Success);

        var result = await _sut.DeleteAccountAsync(new DeleteAccountInput("correct"));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task DeleteAccount_WrongPassword_ReturnsFailure()
    {
        _userContext.UserId.Returns(_userId);
        var user = new ApplicationUser { Id = _userId, UserName = "test@example.com" };
        _userManager.FindByIdAsync(_userId.ToString()).Returns(user);
        _userManager.CheckPasswordAsync(user, "wrong").Returns(false);

        var result = await _sut.DeleteAccountAsync(new DeleteAccountInput("wrong"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.User.DeleteInvalidPassword, result.Error);
    }

    [Fact]
    public async Task DeleteAccount_NotAuthenticated_ReturnsUnauthorized()
    {
        _userContext.UserId.Returns((Guid?)null);

        var result = await _sut.DeleteAccountAsync(new DeleteAccountInput("password"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.User.NotAuthenticated, result.Error);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task DeleteAccount_LastAdmin_ReturnsFailure()
    {
        var user = new ApplicationUser { Id = _userId, UserName = "admin@example.com" };
        _userContext.UserId.Returns(_userId);
        _userManager.FindByIdAsync(_userId.ToString()).Returns(user);
        _userManager.CheckPasswordAsync(user, "correct").Returns(true);
        _userManager.GetRolesAsync(user).Returns(new List<string> { AppRoles.Admin });

        // Set up single admin in role
        var adminRole = new ApplicationRole { Id = Guid.NewGuid(), Name = AppRoles.Admin };
        _roleManager.FindByNameAsync(AppRoles.Admin).Returns(adminRole);
        _dbContext.UserRoles.Add(new IdentityUserRole<Guid> { RoleId = adminRole.Id, UserId = _userId });
        await _dbContext.SaveChangesAsync();

        var result = await _sut.DeleteAccountAsync(new DeleteAccountInput("correct"));

        Assert.True(result.IsFailure);
        Assert.Contains("last user", result.Error);
    }

    [Fact]
    public async Task DeleteAccount_Valid_RevokesTokensAndClearsState()
    {
        var user = new ApplicationUser { Id = _userId, UserName = "test@example.com" };
        _userContext.UserId.Returns(_userId);
        _userManager.FindByIdAsync(_userId.ToString()).Returns(user);
        _userManager.CheckPasswordAsync(user, "correct").Returns(true);
        _userManager.GetRolesAsync(user).Returns(new List<string> { AppRoles.User });
        _userManager.DeleteAsync(user).Returns(IdentityResult.Success);

        // Seed a refresh token
        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "hashed-token",
            UserId = _userId,
            CreatedAt = DateTime.UtcNow,
            ExpiredAt = DateTime.UtcNow.AddDays(7),
            IsUsed = false,
            IsInvalidated = false
        });
        await _dbContext.SaveChangesAsync();

        var result = await _sut.DeleteAccountAsync(new DeleteAccountInput("correct"));

        Assert.True(result.IsSuccess);

        // Verify refresh tokens were invalidated
        var token = Assert.Single(_dbContext.RefreshTokens);
        Assert.True(token.IsInvalidated);

        // Verify cookies were cleared
        _cookieService.Received(1).DeleteCookie(CookieNames.AccessToken);
        _cookieService.Received(1).DeleteCookie(CookieNames.RefreshToken);

        // Verify cache was invalidated
        await _cacheService.Received(1).RemoveAsync(CacheKeys.User(_userId));
    }

    #endregion

    #region GetUserRoles

    [Fact]
    public async Task GetUserRoles_UserNotFound_ReturnsEmptyList()
    {
        _userManager.FindByIdAsync(Arg.Any<string>()).Returns((ApplicationUser?)null);

        var roles = await _sut.GetUserRolesAsync(Guid.NewGuid());

        Assert.Empty(roles);
    }

    [Fact]
    public async Task GetUserRoles_UserExists_ReturnsRoles()
    {
        var user = new ApplicationUser { Id = _userId };
        _userManager.FindByIdAsync(_userId.ToString()).Returns(user);
        _userManager.GetRolesAsync(user).Returns(new List<string> { "User", "Admin" });

        var roles = await _sut.GetUserRolesAsync(_userId);

        Assert.Equal(2, roles.Count);
        Assert.Contains("User", roles);
        Assert.Contains("Admin", roles);
    }

    #endregion
}
