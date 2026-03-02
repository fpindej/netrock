using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using MyProject.Application.Caching.Constants;
using MyProject.Application.Features.Audit;
using MyProject.Application.Features.Authentication;
using MyProject.Application.Features.Authentication.Dtos;
using MyProject.Application.Identity;
using MyProject.Application.Identity.Constants;
using MyProject.Component.Tests.Fixtures;
using MyProject.Infrastructure.Cryptography;
using MyProject.Infrastructure.Features.Authentication.Models;
using MyProject.Infrastructure.Features.Authentication.Options;
using MyProject.Infrastructure.Features.Authentication.Services;
using MyProject.Infrastructure.Features.Authentication.Services.ExternalProviders;
using MyProject.Infrastructure.Persistence;
using MyProject.Shared;

namespace MyProject.Component.Tests.Services;

public class ExternalAuthServiceTests : IDisposable
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly FakeTimeProvider _timeProvider;
    private readonly IUserContext _userContext;
    private readonly IAuditService _auditService;
    private readonly HybridCache _hybridCache;
    private readonly MyProjectDbContext _dbContext;
    private readonly IExternalAuthProvider _googleProvider;
    private readonly IProviderConfigService _providerConfigService;
    private readonly ExternalAuthService _sut;

    private static readonly DateTimeOffset FixedTime = new(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);

    public ExternalAuthServiceTests()
    {
        _userManager = IdentityMockHelpers.CreateMockUserManager();
        _timeProvider = new FakeTimeProvider(FixedTime);
        _userContext = Substitute.For<IUserContext>();
        _auditService = Substitute.For<IAuditService>();
        _hybridCache = Substitute.For<HybridCache>();
        _dbContext = TestDbContextFactory.Create();

        _googleProvider = Substitute.For<IExternalAuthProvider>();
        _googleProvider.Name.Returns("Google");
        _googleProvider.DisplayName.Returns("Google");

        _providerConfigService = Substitute.For<IProviderConfigService>();
        _providerConfigService.GetCredentialsAsync("Google", Arg.Any<CancellationToken>())
            .Returns(new ProviderCredentialsOutput("google-client-id", "google-client-secret"));

        var externalOptions = Options.Create(new ExternalAuthOptions
        {
            AllowedRedirectUris = ["https://example.com/oauth/callback"],
            StateLifetime = TimeSpan.FromMinutes(10)
        });

        var tokenSessionService = Substitute.For<ITokenSessionService>();
        tokenSessionService
            .GenerateTokensAsync(Arg.Any<ApplicationUser>(), Arg.Any<bool>(), Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(new AuthenticationOutput("test-access-token", "test-refresh-token"));

        _sut = new ExternalAuthService(
            _userManager,
            _timeProvider,
            _userContext,
            _auditService,
            _hybridCache,
            tokenSessionService,
            externalOptions,
            [_googleProvider],
            _providerConfigService,
            Substitute.For<ILogger<ExternalAuthService>>(),
            _dbContext);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _userManager.Dispose();
    }

    private ApplicationUser CreateTestUser(Guid? id = null, string? email = null, bool emailConfirmed = true) => new()
    {
        Id = id ?? Guid.NewGuid(),
        UserName = email ?? "test@example.com",
        Email = email ?? "test@example.com",
        EmailConfirmed = emailConfirmed
    };

    private async Task<string> SeedStateAsync(
        string provider = "Google",
        string redirectUri = "https://example.com/oauth/callback",
        Guid? userId = null,
        bool isUsed = false,
        bool isExpired = false)
    {
        var plainToken = "test-state-" + Guid.NewGuid().ToString("N");
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;

        _dbContext.ExternalAuthStates.Add(new ExternalAuthState
        {
            Id = Guid.NewGuid(),
            Token = HashHelper.Sha256(plainToken),
            Provider = provider,
            RedirectUri = redirectUri,
            UserId = userId,
            CreatedAt = utcNow,
            ExpiresAt = isExpired ? utcNow.AddMinutes(-1) : utcNow.AddMinutes(10),
            IsUsed = isUsed
        });

        await _dbContext.SaveChangesAsync();
        return plainToken;
    }

    // ────────── CreateChallengeAsync ──────────

    #region CreateChallenge

    [Fact]
    public async Task CreateChallenge_ValidInput_CreatesStateAndReturnsUrl()
    {
        _googleProvider.BuildAuthorizationUrl(Arg.Any<ProviderCredentials>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns("https://accounts.google.com/o/oauth2/v2/auth?state=abc");

        var input = new ExternalChallengeInput("Google", "https://example.com/oauth/callback");
        var result = await _sut.CreateChallengeAsync(input, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.StartsWith("https://accounts.google.com", result.Value.AuthorizationUrl);

        var stateCount = await _dbContext.ExternalAuthStates.CountAsync();
        Assert.Equal(1, stateCount);
    }

    [Fact]
    public async Task CreateChallenge_InvalidRedirectUri_ReturnsValidationError()
    {
        var input = new ExternalChallengeInput("Google", "https://evil.com/callback");
        var result = await _sut.CreateChallengeAsync(input, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorMessages.ExternalAuth.InvalidRedirectUri, result.Error);
    }

    [Fact]
    public async Task CreateChallenge_UnknownProvider_ReturnsValidationError()
    {
        var input = new ExternalChallengeInput("Unknown", "https://example.com/oauth/callback");
        var result = await _sut.CreateChallengeAsync(input, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorMessages.ExternalAuth.ProviderNotConfigured, result.Error);
    }

    [Fact]
    public async Task CreateChallenge_AuthenticatedUser_StoresUserIdInState()
    {
        var userId = Guid.NewGuid();
        _userContext.UserId.Returns(userId);
        _googleProvider.BuildAuthorizationUrl(Arg.Any<ProviderCredentials>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns("https://accounts.google.com/auth");

        var input = new ExternalChallengeInput("Google", "https://example.com/oauth/callback");
        await _sut.CreateChallengeAsync(input, CancellationToken.None);

        var state = await _dbContext.ExternalAuthStates.SingleAsync();
        Assert.Equal(userId, state.UserId);
    }

    #endregion

    // ────────── HandleCallbackAsync - State Validation ──────────

    #region StateValidation

    [Fact]
    public async Task HandleCallback_InvalidState_ReturnsError()
    {
        var input = new ExternalCallbackInput("code", "nonexistent-state");
        var result = await _sut.HandleCallbackAsync(input, false, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorMessages.ExternalAuth.InvalidState, result.Error);
    }

    [Fact]
    public async Task HandleCallback_UsedState_ReturnsError()
    {
        var stateToken = await SeedStateAsync(isUsed: true);

        var input = new ExternalCallbackInput("code", stateToken);
        var result = await _sut.HandleCallbackAsync(input, false, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorMessages.ExternalAuth.InvalidState, result.Error);
    }

    [Fact]
    public async Task HandleCallback_ExpiredState_ReturnsError()
    {
        var stateToken = await SeedStateAsync(isExpired: true);

        var input = new ExternalCallbackInput("code", stateToken);
        var result = await _sut.HandleCallbackAsync(input, false, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorMessages.ExternalAuth.StateExpired, result.Error);
    }

    [Fact]
    public async Task HandleCallback_ValidState_MarksAsUsed()
    {
        var stateToken = await SeedStateAsync();
        SetupProviderExchange();
        SetupNewUserCreation();

        var input = new ExternalCallbackInput("code", stateToken);
        await _sut.HandleCallbackAsync(input, false, CancellationToken.None);

        var state = await _dbContext.ExternalAuthStates.SingleAsync();
        Assert.True(state.IsUsed);
    }

    #endregion

    // ────────── HandleCallbackAsync - Code Exchange Failure ──────────

    #region CodeExchangeFailure

    [Fact]
    public async Task HandleCallback_CodeExchangeFails_ReturnsError()
    {
        var stateToken = await SeedStateAsync();
        _googleProvider.ExchangeCodeAsync(
                Arg.Any<ProviderCredentials>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<ExternalUserInfo>(_ => throw new HttpRequestException("Provider unreachable"));

        var input = new ExternalCallbackInput("bad-code", stateToken);
        var result = await _sut.HandleCallbackAsync(input, false, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorMessages.ExternalAuth.CodeExchangeFailed, result.Error);
    }

    #endregion

    // ────────── HandleCallbackAsync - Existing Link ──────────

    #region ExistingLink

    [Fact]
    public async Task HandleCallback_ExistingLink_NoSession_LogsIn()
    {
        var user = CreateTestUser();
        var stateToken = await SeedStateAsync();
        SetupProviderExchange("google-123");
        SeedUserLogin(user, "Google", "google-123");

        _userManager.FindByIdAsync(user.Id.ToString()).Returns(user);
        _userContext.UserId.Returns((Guid?)null);

        var input = new ExternalCallbackInput("code", stateToken);
        var result = await _sut.HandleCallbackAsync(input, false, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.Tokens);
        Assert.False(result.Value.IsNewUser);
        Assert.False(result.Value.IsLinkOnly);
    }

    [Fact]
    public async Task HandleCallback_ExistingLink_SameUser_ReturnsLinkOnly()
    {
        var user = CreateTestUser();
        var stateToken = await SeedStateAsync(userId: user.Id);
        SetupProviderExchange("google-123");
        SeedUserLogin(user, "Google", "google-123");

        _userManager.FindByIdAsync(user.Id.ToString()).Returns(user);
        _userContext.UserId.Returns((Guid?)null);

        var input = new ExternalCallbackInput("code", stateToken);
        var result = await _sut.HandleCallbackAsync(input, false, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsLinkOnly);
        Assert.Null(result.Value.Tokens);
    }

    [Fact]
    public async Task HandleCallback_ExistingLink_NoSession_LockedOut_ReturnsError()
    {
        var user = CreateTestUser();
        var stateToken = await SeedStateAsync();
        SetupProviderExchange("google-123");
        SeedUserLogin(user, "Google", "google-123");

        _userManager.FindByIdAsync(user.Id.ToString()).Returns(user);
        _userManager.IsLockedOutAsync(user).Returns(true);
        _userContext.UserId.Returns((Guid?)null);

        var input = new ExternalCallbackInput("code", stateToken);
        var result = await _sut.HandleCallbackAsync(input, false, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorMessages.Auth.LoginAccountLocked, result.Error);
    }

    [Fact]
    public async Task HandleCallback_ExistingLink_DifferentUser_ReturnsError()
    {
        var linkedUser = CreateTestUser();
        var currentUserId = Guid.NewGuid();
        var stateToken = await SeedStateAsync(userId: currentUserId);
        SetupProviderExchange("google-123");
        SeedUserLogin(linkedUser, "Google", "google-123");

        _userManager.FindByIdAsync(linkedUser.Id.ToString()).Returns(linkedUser);

        var input = new ExternalCallbackInput("code", stateToken);
        var result = await _sut.HandleCallbackAsync(input, false, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorMessages.ExternalAuth.AlreadyLinkedToOtherUser, result.Error);
    }

    #endregion

    // ────────── HandleCallbackAsync - No Existing Link (Authenticated) ──────────

    #region LinkToAuthenticated

    [Fact]
    public async Task HandleCallback_Authenticated_LinksProvider()
    {
        var user = CreateTestUser();
        var stateToken = await SeedStateAsync(userId: user.Id);
        SetupProviderExchange();

        _userManager.FindByIdAsync(user.Id.ToString()).Returns(user);
        _userManager.AddLoginAsync(user, Arg.Any<UserLoginInfo>())
            .Returns(IdentityResult.Success);

        var input = new ExternalCallbackInput("code", stateToken);
        var result = await _sut.HandleCallbackAsync(input, false, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsLinkOnly);
        await _auditService.Received(1).LogAsync(
            AuditActions.ExternalAccountLinked,
            user.Id,
            Arg.Any<string?>(),
            Arg.Any<Guid?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleCallback_Authenticated_LinksProvider_InvalidatesCacheAsync()
    {
        var user = CreateTestUser();
        var stateToken = await SeedStateAsync(userId: user.Id);
        SetupProviderExchange();

        _userManager.FindByIdAsync(user.Id.ToString()).Returns(user);
        _userManager.AddLoginAsync(user, Arg.Any<UserLoginInfo>())
            .Returns(IdentityResult.Success);

        var input = new ExternalCallbackInput("code", stateToken);
        await _sut.HandleCallbackAsync(input, false, CancellationToken.None);

        await _hybridCache.Received(1).RemoveAsync(CacheKeys.User(user.Id), Arg.Any<CancellationToken>());
    }

    #endregion

    // ────────── HandleCallbackAsync - No Existing Link (Unauthenticated) ──────────

    #region UnauthenticatedFlow

    [Fact]
    public async Task HandleCallback_Unauthenticated_ExistingUserVerifiedEmail_AutoLinks()
    {
        var user = CreateTestUser(emailConfirmed: true);
        var stateToken = await SeedStateAsync();
        SetupProviderExchange(email: user.Email!);

        _userContext.UserId.Returns((Guid?)null);
        _userManager.FindByEmailAsync(user.Email!).Returns(user);
        _userManager.AddLoginAsync(user, Arg.Any<UserLoginInfo>())
            .Returns(IdentityResult.Success);

        var input = new ExternalCallbackInput("code", stateToken);
        var result = await _sut.HandleCallbackAsync(input, false, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsNewUser);
        Assert.NotNull(result.Value.Tokens);
    }

    [Fact]
    public async Task HandleCallback_Unauthenticated_ProviderEmailNotVerified_ReturnsError()
    {
        var user = CreateTestUser(emailConfirmed: true);
        var stateToken = await SeedStateAsync();
        SetupProviderExchange(email: user.Email!, emailVerified: false);

        _userContext.UserId.Returns((Guid?)null);
        _userManager.FindByEmailAsync(user.Email!).Returns(user);

        var input = new ExternalCallbackInput("code", stateToken);
        var result = await _sut.HandleCallbackAsync(input, false, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorMessages.ExternalAuth.EmailNotVerified, result.Error);
    }

    [Fact]
    public async Task HandleCallback_Unauthenticated_AutoLink_LockedOut_ReturnsError()
    {
        var user = CreateTestUser(emailConfirmed: true);
        var stateToken = await SeedStateAsync();
        SetupProviderExchange(email: user.Email!);

        _userContext.UserId.Returns((Guid?)null);
        _userManager.FindByEmailAsync(user.Email!).Returns(user);
        _userManager.IsLockedOutAsync(user).Returns(true);

        var input = new ExternalCallbackInput("code", stateToken);
        var result = await _sut.HandleCallbackAsync(input, false, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorMessages.Auth.LoginAccountLocked, result.Error);
    }

    [Fact]
    public async Task HandleCallback_Unauthenticated_AutoLink_InvalidatesCacheAsync()
    {
        var user = CreateTestUser(emailConfirmed: true);
        var stateToken = await SeedStateAsync();
        SetupProviderExchange(email: user.Email!);

        _userContext.UserId.Returns((Guid?)null);
        _userManager.FindByEmailAsync(user.Email!).Returns(user);
        _userManager.AddLoginAsync(user, Arg.Any<UserLoginInfo>())
            .Returns(IdentityResult.Success);

        var input = new ExternalCallbackInput("code", stateToken);
        await _sut.HandleCallbackAsync(input, false, CancellationToken.None);

        await _hybridCache.Received(1).RemoveAsync(CacheKeys.User(user.Id), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleCallback_Unauthenticated_ExistingUserUnverifiedEmail_ReturnsError()
    {
        var user = CreateTestUser(emailConfirmed: false);
        var stateToken = await SeedStateAsync();
        SetupProviderExchange(email: user.Email!);

        _userContext.UserId.Returns((Guid?)null);
        _userManager.FindByEmailAsync(user.Email!).Returns(user);

        var input = new ExternalCallbackInput("code", stateToken);
        var result = await _sut.HandleCallbackAsync(input, false, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorMessages.ExternalAuth.EmailNotVerified, result.Error);
    }

    [Fact]
    public async Task HandleCallback_Unauthenticated_NoExistingUser_CreatesAccount()
    {
        var stateToken = await SeedStateAsync();
        SetupProviderExchange(email: "new@example.com");
        SetupNewUserCreation();

        _userContext.UserId.Returns((Guid?)null);
        _userManager.FindByEmailAsync("new@example.com").Returns((ApplicationUser?)null);

        var input = new ExternalCallbackInput("code", stateToken);
        var result = await _sut.HandleCallbackAsync(input, false, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsNewUser);
        Assert.NotNull(result.Value.Tokens);
        await _auditService.Received(1).LogAsync(
            AuditActions.ExternalAccountCreated,
            Arg.Any<Guid?>(),
            Arg.Any<string?>(),
            Arg.Any<Guid?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleCallback_CreateUser_AddLoginFails_DeletesOrphanUser()
    {
        var stateToken = await SeedStateAsync();
        SetupProviderExchange(email: "new@example.com");

        _userContext.UserId.Returns((Guid?)null);
        _userManager.FindByEmailAsync("new@example.com").Returns((ApplicationUser?)null);
        _userManager.CreateAsync(Arg.Any<ApplicationUser>()).Returns(IdentityResult.Success);
        _userManager.AddToRoleAsync(Arg.Any<ApplicationUser>(), AppRoles.User).Returns(IdentityResult.Success);
        _userManager.AddLoginAsync(Arg.Any<ApplicationUser>(), Arg.Any<UserLoginInfo>())
            .Returns(IdentityResult.Failed(new IdentityError { Description = "Link failed" }));

        var input = new ExternalCallbackInput("code", stateToken);
        var result = await _sut.HandleCallbackAsync(input, false, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorMessages.ExternalAuth.ProviderError, result.Error);
        await _userManager.Received(1).DeleteAsync(Arg.Any<ApplicationUser>());
    }

    #endregion

    // ────────── UnlinkProviderAsync ──────────

    #region Unlink

    [Fact]
    public async Task Unlink_ValidProvider_WithPassword_Succeeds()
    {
        var user = CreateTestUser();
        _userContext.UserId.Returns(user.Id);
        _userManager.FindByIdAsync(user.Id.ToString()).Returns(user);
        _userManager.GetLoginsAsync(user).Returns(
            [new UserLoginInfo("Google", "google-123", "Google")]);
        _userManager.HasPasswordAsync(user).Returns(true);
        _userManager.RemoveLoginAsync(user, "Google", "google-123").Returns(IdentityResult.Success);

        var result = await _sut.UnlinkProviderAsync("Google", CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _hybridCache.Received(1).RemoveAsync(CacheKeys.User(user.Id), Arg.Any<CancellationToken>());
        await _auditService.Received(1).LogAsync(
            AuditActions.ExternalAccountUnlinked,
            user.Id,
            Arg.Any<string?>(),
            Arg.Any<Guid?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Unlink_LastAuthMethod_NoPassword_ReturnsError()
    {
        var user = CreateTestUser();
        _userContext.UserId.Returns(user.Id);
        _userManager.FindByIdAsync(user.Id.ToString()).Returns(user);
        _userManager.GetLoginsAsync(user).Returns(
            [new UserLoginInfo("Google", "google-123", "Google")]);
        _userManager.HasPasswordAsync(user).Returns(false);

        var result = await _sut.UnlinkProviderAsync("Google", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorMessages.ExternalAuth.CannotUnlinkLastMethod, result.Error);
    }

    [Fact]
    public async Task Unlink_MultipleProviders_NoPassword_Succeeds()
    {
        var user = CreateTestUser();
        _userContext.UserId.Returns(user.Id);
        _userManager.FindByIdAsync(user.Id.ToString()).Returns(user);
        _userManager.GetLoginsAsync(user).Returns(
        [
            new UserLoginInfo("Google", "google-123", "Google"),
            new UserLoginInfo("GitHub", "github-456", "GitHub")
        ]);
        _userManager.HasPasswordAsync(user).Returns(false);
        _userManager.RemoveLoginAsync(user, "Google", "google-123").Returns(IdentityResult.Success);

        var result = await _sut.UnlinkProviderAsync("Google", CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Unlink_ProviderNotLinked_ReturnsError()
    {
        var user = CreateTestUser();
        _userContext.UserId.Returns(user.Id);
        _userManager.FindByIdAsync(user.Id.ToString()).Returns(user);
        _userManager.GetLoginsAsync(user).Returns([]);

        var result = await _sut.UnlinkProviderAsync("Google", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorMessages.ExternalAuth.ProviderNotLinked, result.Error);
    }

    [Fact]
    public async Task Unlink_NotAuthenticated_ReturnsError()
    {
        _userContext.UserId.Returns((Guid?)null);

        var result = await _sut.UnlinkProviderAsync("Google", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorMessages.Auth.NotAuthenticated, result.Error);
    }

    #endregion

    // ────────── SetPasswordAsync ──────────

    #region SetPassword

    [Fact]
    public async Task SetPassword_NoExistingPassword_Succeeds()
    {
        var user = CreateTestUser();
        _userContext.UserId.Returns(user.Id);
        _userManager.FindByIdAsync(user.Id.ToString()).Returns(user);
        _userManager.HasPasswordAsync(user).Returns(false);
        _userManager.AddPasswordAsync(user, "NewPassword1!").Returns(IdentityResult.Success);

        var input = new SetPasswordInput("NewPassword1!");
        var result = await _sut.SetPasswordAsync(input, CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _hybridCache.Received(1).RemoveAsync(CacheKeys.User(user.Id), Arg.Any<CancellationToken>());
        await _auditService.Received(1).LogAsync(
            AuditActions.PasswordSet,
            user.Id,
            Arg.Any<string?>(),
            Arg.Any<Guid?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetPassword_AlreadyHasPassword_ReturnsError()
    {
        var user = CreateTestUser();
        _userContext.UserId.Returns(user.Id);
        _userManager.FindByIdAsync(user.Id.ToString()).Returns(user);
        _userManager.HasPasswordAsync(user).Returns(true);

        var input = new SetPasswordInput("NewPassword1!");
        var result = await _sut.SetPasswordAsync(input, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorMessages.ExternalAuth.PasswordAlreadySet, result.Error);
    }

    [Fact]
    public async Task SetPassword_NotAuthenticated_ReturnsError()
    {
        _userContext.UserId.Returns((Guid?)null);

        var input = new SetPasswordInput("NewPassword1!");
        var result = await _sut.SetPasswordAsync(input, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorMessages.Auth.NotAuthenticated, result.Error);
    }

    [Fact]
    public async Task SetPassword_AddPasswordFails_ReturnsError()
    {
        var user = CreateTestUser();
        _userContext.UserId.Returns(user.Id);
        _userManager.FindByIdAsync(user.Id.ToString()).Returns(user);
        _userManager.HasPasswordAsync(user).Returns(false);
        _userManager.AddPasswordAsync(user, Arg.Any<string>())
            .Returns(IdentityResult.Failed(new IdentityError { Description = "Too weak" }));

        var input = new SetPasswordInput("weak");
        var result = await _sut.SetPasswordAsync(input, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorMessages.ExternalAuth.PasswordSetFailed, result.Error);
    }

    #endregion

    // ────────── GetAvailableProviders ──────────

    #region GetAvailableProviders

    [Fact]
    public async Task GetAvailableProviders_ReturnsConfiguredProviders()
    {
        _providerConfigService.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns([new ProviderConfigOutput("Google", "Google", true, "client-id", true, "database", null, null)]);

        var providers = await _sut.GetAvailableProvidersAsync(CancellationToken.None);

        Assert.Single(providers);
        Assert.Equal("Google", providers[0].Name);
        Assert.Equal("Google", providers[0].DisplayName);
    }

    #endregion

    // ────────── GetLinkedProvidersAsync ──────────

    #region GetLinkedProviders

    [Fact]
    public async Task GetLinkedProviders_UserWithLogins_ReturnsProviderNames()
    {
        var user = CreateTestUser();
        _userManager.FindByIdAsync(user.Id.ToString()).Returns(user);
        _userManager.GetLoginsAsync(user).Returns(
        [
            new UserLoginInfo("Google", "google-123", "Google"),
            new UserLoginInfo("GitHub", "github-456", "GitHub")
        ]);

        var providers = await _sut.GetLinkedProvidersAsync(user.Id, CancellationToken.None);

        Assert.Equal(2, providers.Count);
        Assert.Contains("Google", providers);
        Assert.Contains("GitHub", providers);
    }

    [Fact]
    public async Task GetLinkedProviders_UserNotFound_ReturnsEmpty()
    {
        _userManager.FindByIdAsync(Arg.Any<string>()).Returns((ApplicationUser?)null);

        var providers = await _sut.GetLinkedProvidersAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Empty(providers);
    }

    #endregion

    // ────────── Helpers ──────────

    private void SetupProviderExchange(
        string providerKey = "google-123",
        string email = "test@example.com",
        bool emailVerified = true)
    {
        _googleProvider.ExchangeCodeAsync(
                Arg.Any<ProviderCredentials>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ExternalUserInfo(providerKey, email, emailVerified, "Test", "User"));
    }

    private void SetupNewUserCreation()
    {
        _userManager.FindByEmailAsync(Arg.Any<string>()).Returns((ApplicationUser?)null);
        _userManager.CreateAsync(Arg.Any<ApplicationUser>()).Returns(IdentityResult.Success);
        _userManager.AddToRoleAsync(Arg.Any<ApplicationUser>(), AppRoles.User).Returns(IdentityResult.Success);
        _userManager.AddLoginAsync(Arg.Any<ApplicationUser>(), Arg.Any<UserLoginInfo>())
            .Returns(IdentityResult.Success);
    }

    private void SeedUserLogin(ApplicationUser user, string provider, string providerKey)
    {
        _dbContext.Set<IdentityUserLogin<Guid>>().Add(new IdentityUserLogin<Guid>
        {
            LoginProvider = provider,
            ProviderKey = providerKey,
            ProviderDisplayName = provider,
            UserId = user.Id
        });
        _dbContext.SaveChanges();

        _userManager.FindByIdAsync(user.Id.ToString()).Returns(user);
    }
}
