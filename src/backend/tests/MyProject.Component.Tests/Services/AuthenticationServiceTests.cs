using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using MyProject.Application.Caching;
using MyProject.Application.Cookies;
using MyProject.Application.Features.Authentication.Dtos;
using MyProject.Application.Identity;
using MyProject.Component.Tests.Fixtures;
using MyProject.Infrastructure.Cryptography;
using MyProject.Infrastructure.Features.Authentication.Models;
using MyProject.Infrastructure.Features.Authentication.Options;
using MyProject.Infrastructure.Features.Authentication.Services;
using MyProject.Infrastructure.Persistence;
using MyProject.Shared;

namespace MyProject.Component.Tests.Services;

public class AuthenticationServiceTests : IDisposable
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITokenProvider _tokenProvider;
    private readonly FakeTimeProvider _timeProvider;
    private readonly ICookieService _cookieService;
    private readonly IUserContext _userContext;
    private readonly ICacheService _cacheService;
    private readonly MyProjectDbContext _dbContext;
    private readonly AuthenticationService _sut;

    public AuthenticationServiceTests()
    {
        _userManager = IdentityMockHelpers.CreateMockUserManager();
        _signInManager = IdentityMockHelpers.CreateMockSignInManager(_userManager);
        _tokenProvider = Substitute.For<ITokenProvider>();
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2025, 1, 15, 12, 0, 0, TimeSpan.Zero));
        _cookieService = Substitute.For<ICookieService>();
        _userContext = Substitute.For<IUserContext>();
        _cacheService = Substitute.For<ICacheService>();
        _dbContext = TestDbContextFactory.Create();

        var authOptions = Options.Create(new AuthenticationOptions
        {
            Jwt = new AuthenticationOptions.JwtOptions
            {
                Key = "ThisIsATestSigningKeyWithAtLeast32Chars!",
                Issuer = "test-issuer",
                Audience = "test-audience",
                ExpiresInMinutes = 10,
                RefreshToken = new AuthenticationOptions.JwtOptions.RefreshTokenOptions
                {
                    ExpiresInDays = 7
                }
            }
        });

        _sut = new AuthenticationService(
            _userManager,
            _signInManager,
            _tokenProvider,
            _timeProvider,
            _cookieService,
            _userContext,
            _cacheService,
            authOptions,
            _dbContext);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _userManager.Dispose();
    }

    #region Login

    [Fact]
    public async Task Login_ValidCredentials_ReturnsSuccessWithTokens()
    {
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = "test@example.com" };
        _userManager.FindByNameAsync("test@example.com").Returns(user);
        _signInManager.CheckPasswordSignInAsync(user, "password123", true)
            .Returns(SignInResult.Success);
        _tokenProvider.GenerateAccessToken(user).Returns("access-token");
        _tokenProvider.GenerateRefreshToken().Returns("refresh-token");

        var result = await _sut.Login("test@example.com", "password123");

        Assert.True(result.IsSuccess);
        Assert.Equal("access-token", result.Value.AccessToken);
        Assert.Equal("refresh-token", result.Value.RefreshToken);
    }

    [Fact]
    public async Task Login_ValidCredentials_StoresRefreshTokenInDatabase()
    {
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = "test@example.com" };
        _userManager.FindByNameAsync("test@example.com").Returns(user);
        _signInManager.CheckPasswordSignInAsync(user, "password123", true)
            .Returns(SignInResult.Success);
        _tokenProvider.GenerateAccessToken(user).Returns("access-token");
        _tokenProvider.GenerateRefreshToken().Returns("refresh-token");

        await _sut.Login("test@example.com", "password123");

        var storedToken = Assert.Single(_dbContext.RefreshTokens);
        Assert.Equal(HashHelper.Sha256("refresh-token"), storedToken.Token);
        Assert.Equal(user.Id, storedToken.UserId);
        Assert.False(storedToken.IsUsed);
        Assert.False(storedToken.IsInvalidated);
    }

    [Fact]
    public async Task Login_InvalidUser_ReturnsUnauthorized()
    {
        _userManager.FindByNameAsync("unknown@example.com").Returns((ApplicationUser?)null);

        var result = await _sut.Login("unknown@example.com", "password123");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Auth.LoginInvalidCredentials, result.Error);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = "test@example.com" };
        _userManager.FindByNameAsync("test@example.com").Returns(user);
        _signInManager.CheckPasswordSignInAsync(user, "wrong", true)
            .Returns(SignInResult.Failed);

        var result = await _sut.Login("test@example.com", "wrong");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Auth.LoginInvalidCredentials, result.Error);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task Login_LockedOut_ReturnsLockedMessage()
    {
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = "test@example.com" };
        _userManager.FindByNameAsync("test@example.com").Returns(user);
        _signInManager.CheckPasswordSignInAsync(user, "password123", true)
            .Returns(SignInResult.LockedOut);

        var result = await _sut.Login("test@example.com", "password123");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Auth.LoginAccountLocked, result.Error);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task Login_WithCookies_SetsCookies()
    {
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = "test@example.com" };
        _userManager.FindByNameAsync("test@example.com").Returns(user);
        _signInManager.CheckPasswordSignInAsync(user, "password123", true)
            .Returns(SignInResult.Success);
        _tokenProvider.GenerateAccessToken(user).Returns("access-token");
        _tokenProvider.GenerateRefreshToken().Returns("refresh-token");

        await _sut.Login("test@example.com", "password123", useCookies: true);

        _cookieService.Received(2).SetSecureCookie(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>());
    }

    #endregion

    #region Register

    [Fact]
    public async Task Register_ValidInput_ReturnsSuccess()
    {
        var input = new RegisterInput("test@example.com", "Password1!", null, null, null);
        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), "Password1!")
            .Returns(callInfo =>
            {
                callInfo.Arg<ApplicationUser>().Id = Guid.NewGuid();
                return IdentityResult.Success;
            });
        _userManager.AddToRoleAsync(Arg.Any<ApplicationUser>(), "User")
            .Returns(IdentityResult.Success);

        var result = await _sut.Register(input);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsFailure()
    {
        var input = new RegisterInput("test@example.com", "Password1!", null, null, null);
        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), "Password1!")
            .Returns(IdentityResult.Failed(new IdentityError { Description = "Duplicate email." }));

        var result = await _sut.Register(input);

        Assert.True(result.IsFailure);
        Assert.Contains("Duplicate email", result.Error);
    }

    [Fact]
    public async Task Register_DuplicatePhone_ReturnsFailure()
    {
        // Seed a user with the same phone into the DbContext (which uses the real InMemory queryable)
        _dbContext.Users.Add(new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "existing@example.com",
            PhoneNumber = "+420123456789"
        });
        await _dbContext.SaveChangesAsync();

        // Point userManager.Users at the DbContext's real Users DbSet
        _userManager.Users.Returns(_dbContext.Users);

        var input = new RegisterInput("test@example.com", "Password1!", null, null, "+420123456789");

        var result = await _sut.Register(input);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.User.PhoneNumberTaken, result.Error);
    }

    #endregion

    #region RefreshToken

    [Fact]
    public async Task RefreshToken_ValidToken_ReturnsNewTokens()
    {
        var userId = Guid.NewGuid();
        var user = new ApplicationUser { Id = userId, UserName = "test@example.com" };
        var hashedToken = HashHelper.Sha256("valid-refresh-token");

        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = hashedToken,
            UserId = userId,
            User = user,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime.AddHours(-1),
            ExpiredAt = _timeProvider.GetUtcNow().UtcDateTime.AddDays(7),
            IsUsed = false,
            IsInvalidated = false
        });
        await _dbContext.SaveChangesAsync();

        _tokenProvider.GenerateAccessToken(user).Returns("new-access-token");
        _tokenProvider.GenerateRefreshToken().Returns("new-refresh-token");

        var result = await _sut.RefreshTokenAsync("valid-refresh-token");

        Assert.True(result.IsSuccess);
        Assert.Equal("new-access-token", result.Value.AccessToken);
        Assert.Equal("new-refresh-token", result.Value.RefreshToken);
    }

    [Fact]
    public async Task RefreshToken_ExpiredToken_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var user = new ApplicationUser { Id = userId, UserName = "expired@test.com" };
        var hashedToken = HashHelper.Sha256("expired-token");

        _dbContext.Users.Add(user);
        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = hashedToken,
            UserId = userId,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime.AddDays(-10),
            ExpiredAt = _timeProvider.GetUtcNow().UtcDateTime.AddDays(-3),
            IsUsed = false,
            IsInvalidated = false
        });
        await _dbContext.SaveChangesAsync();

        var result = await _sut.RefreshTokenAsync("expired-token");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Auth.TokenExpired, result.Error);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task RefreshToken_InvalidatedToken_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var user = new ApplicationUser { Id = userId, UserName = "invalidated@test.com" };
        var hashedToken = HashHelper.Sha256("invalidated-token");

        _dbContext.Users.Add(user);
        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = hashedToken,
            UserId = userId,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime.AddHours(-1),
            ExpiredAt = _timeProvider.GetUtcNow().UtcDateTime.AddDays(7),
            IsUsed = false,
            IsInvalidated = true
        });
        await _dbContext.SaveChangesAsync();

        var result = await _sut.RefreshTokenAsync("invalidated-token");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Auth.TokenInvalidated, result.Error);
    }

    [Fact]
    public async Task RefreshToken_ReusedToken_RevokesAllUserTokens()
    {
        var userId = Guid.NewGuid();
        var user = new ApplicationUser { Id = userId, UserName = "reused@test.com" };
        var hashedToken = HashHelper.Sha256("reused-token");

        _dbContext.Users.Add(user);
        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = hashedToken,
            UserId = userId,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime.AddHours(-1),
            ExpiredAt = _timeProvider.GetUtcNow().UtcDateTime.AddDays(7),
            IsUsed = true,
            IsInvalidated = false
        });
        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = HashHelper.Sha256("other-token"),
            UserId = userId,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
            ExpiredAt = _timeProvider.GetUtcNow().UtcDateTime.AddDays(7),
            IsUsed = false,
            IsInvalidated = false
        });
        await _dbContext.SaveChangesAsync();

        var result = await _sut.RefreshTokenAsync("reused-token");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Auth.TokenReused, result.Error);
    }

    [Fact]
    public async Task RefreshToken_EmptyString_ReturnsTokenMissing()
    {
        var result = await _sut.RefreshTokenAsync("");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Auth.TokenMissing, result.Error);
    }

    [Fact]
    public async Task RefreshToken_NotFound_ReturnsFailure()
    {
        var result = await _sut.RefreshTokenAsync("nonexistent-token");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Auth.TokenNotFound, result.Error);
    }

    #endregion

    #region ChangePassword

    [Fact]
    public async Task ChangePassword_Valid_ReturnsSuccess()
    {
        var userId = Guid.NewGuid();
        var user = new ApplicationUser { Id = userId, UserName = "test@example.com" };
        _userContext.UserId.Returns(userId);
        _userManager.FindByIdAsync(userId.ToString()).Returns(user);
        _userManager.CheckPasswordAsync(user, "current").Returns(true);
        _userManager.ChangePasswordAsync(user, "current", "newPass1!")
            .Returns(IdentityResult.Success);

        var result = await _sut.ChangePasswordAsync(new ChangePasswordInput("current", "newPass1!"));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var user = new ApplicationUser { Id = userId };
        _userContext.UserId.Returns(userId);
        _userManager.FindByIdAsync(userId.ToString()).Returns(user);
        _userManager.CheckPasswordAsync(user, "wrong").Returns(false);

        var result = await _sut.ChangePasswordAsync(new ChangePasswordInput("wrong", "newPass1!"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Auth.PasswordIncorrect, result.Error);
    }

    [Fact]
    public async Task ChangePassword_NotAuthenticated_ReturnsUnauthorized()
    {
        _userContext.UserId.Returns((Guid?)null);

        var result = await _sut.ChangePasswordAsync(new ChangePasswordInput("current", "newPass1!"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Auth.NotAuthenticated, result.Error);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    #endregion

    #region Logout

    [Fact]
    public async Task Logout_WithAuthenticatedUser_ClearsCookiesAndRevokesTokens()
    {
        var userId = Guid.NewGuid();
        _userContext.UserId.Returns(userId);
        _userManager.FindByIdAsync(userId.ToString())
            .Returns(new ApplicationUser { Id = userId });

        await _sut.Logout();

        _cookieService.Received(2).DeleteCookie(Arg.Any<string>());
    }

    [Fact]
    public async Task Logout_WithoutAuthenticatedUser_ClearsCookiesOnly()
    {
        _userContext.UserId.Returns((Guid?)null);

        await _sut.Logout();

        _cookieService.Received(2).DeleteCookie(Arg.Any<string>());
    }

    #endregion
}
