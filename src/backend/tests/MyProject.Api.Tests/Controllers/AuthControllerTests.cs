using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MyProject.Api.Tests.Contracts;
using MyProject.Api.Tests.Fixtures;
using MyProject.Application.Cookies.Constants;
using MyProject.Application.Features.Authentication.Dtos;
using MyProject.Shared;

namespace MyProject.Api.Tests.Controllers;

public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory>, IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetMocks();
        _client = factory.CreateClient();
    }

    public void Dispose() => _client.Dispose();

    private static HttpRequestMessage Post(string url, HttpContent? content = null, string? auth = null)
    {
        var msg = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        if (auth is not null) msg.Headers.Add("Authorization", auth);
        return msg;
    }

    private static async Task AssertProblemDetailsAsync(
        HttpResponseMessage response, int expectedStatus, string? expectedDetail = null)
    {
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(expectedStatus, json.GetProperty("status").GetInt32());
        if (expectedDetail is not null)
        {
            Assert.Equal(expectedDetail, json.GetProperty("detail").GetString());
        }
    }

    #region Login

    [Fact]
    public async Task Login_ValidCredentials_Returns200()
    {
        _factory.AuthenticationService.Login(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Result<LoginOutput>.Success(new LoginOutput(
                Tokens: new AuthenticationOutput("access", "refresh"),
                ChallengeToken: null,
                RequiresTwoFactor: false)));

        var response = await _client.SendAsync(
            Post("/api/auth/login", JsonContent.Create(new { Username = "test@example.com", Password = "Password1!" })));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthTokensResponse>();
        Assert.NotNull(body);
        Assert.NotEmpty(body.AccessToken);
        Assert.NotEmpty(body.RefreshToken);
    }

    [Fact]
    public async Task Login_InvalidCredentials_Returns401WithProblemDetails()
    {
        _factory.AuthenticationService.Login(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Result<LoginOutput>.Failure(
                ErrorMessages.Auth.LoginInvalidCredentials, ErrorType.Unauthorized));

        var response = await _client.SendAsync(
            Post("/api/auth/login", JsonContent.Create(new { Username = "test@example.com", Password = "wrong" })));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await AssertProblemDetailsAsync(response, 401, ErrorMessages.Auth.LoginInvalidCredentials);
    }

    [Fact]
    public async Task Login_MissingEmail_Returns400()
    {
        var response = await _client.SendAsync(
            Post("/api/auth/login", JsonContent.Create(new { Username = "", Password = "Password1!" })));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Register

    [Fact]
    public async Task Register_ValidInput_Returns201()
    {
        _factory.CaptchaService.ValidateTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _factory.AuthenticationService.Register(Arg.Any<RegisterInput>(), Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        var response = await _client.SendAsync(
            Post("/api/auth/register", JsonContent.Create(new { Email = "new@example.com", Password = "Password1!", CaptchaToken = "valid-token" })));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RegisterUserResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.Id);
    }

    [Fact]
    public async Task Register_InvalidEmail_Returns400()
    {
        var response = await _client.SendAsync(
            Post("/api/auth/register", JsonContent.Create(new { Email = "not-an-email", Password = "Password1!", CaptchaToken = "valid-token" })));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WeakPassword_Returns400()
    {
        var response = await _client.SendAsync(
            Post("/api/auth/register", JsonContent.Create(new { Email = "test@example.com", Password = "weak", CaptchaToken = "valid-token" })));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_ServiceFailure_Returns400WithProblemDetails()
    {
        _factory.CaptchaService.ValidateTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _factory.AuthenticationService.Register(Arg.Any<RegisterInput>(), Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Failure("Email already registered."));

        var response = await _client.SendAsync(
            Post("/api/auth/register", JsonContent.Create(new { Email = "dup@example.com", Password = "Password1!", CaptchaToken = "valid-token" })));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemDetailsAsync(response, 400, "Email already registered.");
    }

    [Fact]
    public async Task Register_InvalidCaptcha_Returns400()
    {
        _factory.CaptchaService.ValidateTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var response = await _client.SendAsync(
            Post("/api/auth/register", JsonContent.Create(new { Email = "new@example.com", Password = "Password1!", CaptchaToken = "invalid-token" })));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemDetailsAsync(response, 400, ErrorMessages.Auth.CaptchaInvalid);
    }

    [Fact]
    public async Task Register_MissingCaptcha_Returns400()
    {
        var response = await _client.SendAsync(
            Post("/api/auth/register", JsonContent.Create(new { Email = "new@example.com", Password = "Password1!" })));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Refresh

    [Fact]
    public async Task Refresh_ValidBodyToken_Returns200()
    {
        _factory.AuthenticationService.RefreshTokenAsync("valid-token", false, Arg.Any<CancellationToken>())
            .Returns(Result<AuthenticationOutput>.Success(new AuthenticationOutput("new-access", "new-refresh")));

        var response = await _client.SendAsync(
            Post("/api/auth/refresh", JsonContent.Create(new { RefreshToken = "valid-token" })));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthTokensResponse>();
        Assert.NotNull(body);
        Assert.NotEmpty(body.AccessToken);
        Assert.NotEmpty(body.RefreshToken);
    }

    [Fact]
    public async Task Refresh_CookieFallback_Returns200()
    {
        _factory.AuthenticationService.RefreshTokenAsync("cookie-token", false, Arg.Any<CancellationToken>())
            .Returns(Result<AuthenticationOutput>.Success(new AuthenticationOutput("new-access", "new-refresh")));

        var request = Post("/api/auth/refresh", JsonContent.Create(new { }));
        request.Headers.Add("Cookie", $"{CookieNames.RefreshToken}=cookie-token");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthTokensResponse>();
        Assert.NotNull(body);
        Assert.NotEmpty(body.AccessToken);
        Assert.NotEmpty(body.RefreshToken);
    }

    [Fact]
    public async Task Refresh_MissingToken_Returns401WithProblemDetails()
    {
        var response = await _client.SendAsync(
            Post("/api/auth/refresh", JsonContent.Create(new { })));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await AssertProblemDetailsAsync(response, 401, ErrorMessages.Auth.TokenMissing);
    }

    [Fact]
    public async Task Refresh_WithUseCookies_PassesFlagToService()
    {
        _factory.AuthenticationService.RefreshTokenAsync("valid-token", true, Arg.Any<CancellationToken>())
            .Returns(Result<AuthenticationOutput>.Success(new AuthenticationOutput("new-access", "new-refresh")));

        var response = await _client.SendAsync(
            Post("/api/auth/refresh?useCookies=true", JsonContent.Create(new { RefreshToken = "valid-token" })));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthTokensResponse>();
        Assert.NotNull(body);
        Assert.NotEmpty(body.AccessToken);
        Assert.NotEmpty(body.RefreshToken);
        await _factory.AuthenticationService.Received(1)
            .RefreshTokenAsync("valid-token", true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Refresh_InvalidToken_Returns401WithProblemDetails()
    {
        _factory.AuthenticationService.RefreshTokenAsync("invalid", false, Arg.Any<CancellationToken>())
            .Returns(Result<AuthenticationOutput>.Failure(
                ErrorMessages.Auth.TokenInvalidated, ErrorType.Unauthorized));

        var response = await _client.SendAsync(
            Post("/api/auth/refresh", JsonContent.Create(new { RefreshToken = "invalid" })));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await AssertProblemDetailsAsync(response, 401, ErrorMessages.Auth.TokenInvalidated);
    }

    [Fact]
    public async Task Refresh_NullBody_WithCookie_Returns200()
    {
        _factory.AuthenticationService.RefreshTokenAsync("cookie-token", false, Arg.Any<CancellationToken>())
            .Returns(Result<AuthenticationOutput>.Success(new AuthenticationOutput("a", "r")));

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", $"{CookieNames.RefreshToken}=cookie-token");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthTokensResponse>();
        Assert.NotNull(body);
        Assert.NotEmpty(body.AccessToken);
        Assert.NotEmpty(body.RefreshToken);
    }

    #endregion

    #region Logout

    [Fact]
    public async Task Logout_Authenticated_Returns204()
    {
        var response = await _client.SendAsync(Post("/api/auth/logout", auth: TestAuth.User()));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Logout_Unauthenticated_Returns401()
    {
        var response = await _client.SendAsync(Post("/api/auth/logout"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await AssertProblemDetailsAsync(response, 401, ErrorMessages.Auth.NotAuthenticated);
    }

    #endregion

    #region ChangePassword

    [Fact]
    public async Task ChangePassword_Authenticated_Returns204()
    {
        _factory.AuthenticationService.ChangePasswordAsync(
                Arg.Any<ChangePasswordInput>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var response = await _client.SendAsync(
            Post("/api/auth/change-password",
                JsonContent.Create(new { CurrentPassword = "OldPass1!", NewPassword = "NewPass1!" }),
                TestAuth.User()));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_Unauthenticated_Returns401()
    {
        var response = await _client.SendAsync(
            Post("/api/auth/change-password",
                JsonContent.Create(new { CurrentPassword = "OldPass1!", NewPassword = "NewPass1!" })));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_ServiceFailure_Returns400WithProblemDetails()
    {
        _factory.AuthenticationService.ChangePasswordAsync(
                Arg.Any<ChangePasswordInput>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure("Current password is incorrect."));

        var response = await _client.SendAsync(
            Post("/api/auth/change-password",
                JsonContent.Create(new { CurrentPassword = "WrongPass1!", NewPassword = "NewPass1!" }),
                TestAuth.User()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemDetailsAsync(response, 400, "Current password is incorrect.");
    }

    #endregion

    #region ForgotPassword

    [Fact]
    public async Task ForgotPassword_ValidEmail_Returns200()
    {
        _factory.CaptchaService.ValidateTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _factory.AuthenticationService.ForgotPasswordAsync(
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var response = await _client.SendAsync(
            Post("/api/auth/forgot-password", JsonContent.Create(new { Email = "test@example.com", CaptchaToken = "valid-token" })));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_InvalidEmail_Returns400()
    {
        var response = await _client.SendAsync(
            Post("/api/auth/forgot-password", JsonContent.Create(new { Email = "not-an-email", CaptchaToken = "valid-token" })));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_EmptyEmail_Returns400()
    {
        var response = await _client.SendAsync(
            Post("/api/auth/forgot-password", JsonContent.Create(new { Email = "", CaptchaToken = "valid-token" })));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_InvalidCaptcha_Returns400()
    {
        _factory.CaptchaService.ValidateTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var response = await _client.SendAsync(
            Post("/api/auth/forgot-password", JsonContent.Create(new { Email = "test@example.com", CaptchaToken = "invalid-token" })));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemDetailsAsync(response, 400, ErrorMessages.Auth.CaptchaInvalid);
    }

    [Fact]
    public async Task ForgotPassword_MissingCaptcha_Returns400()
    {
        var response = await _client.SendAsync(
            Post("/api/auth/forgot-password", JsonContent.Create(new { Email = "test@example.com" })));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region ResetPassword

    [Fact]
    public async Task ResetPassword_ValidInput_Returns200()
    {
        _factory.AuthenticationService.ResetPasswordAsync(
                Arg.Any<ResetPasswordInput>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var response = await _client.SendAsync(
            Post("/api/auth/reset-password", JsonContent.Create(new
            {
                Token = "valid-token",
                NewPassword = "NewPassword1!"
            })));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_InvalidToken_Returns400WithProblemDetails()
    {
        _factory.AuthenticationService.ResetPasswordAsync(
                Arg.Any<ResetPasswordInput>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(ErrorMessages.Auth.ResetPasswordTokenInvalid));

        var response = await _client.SendAsync(
            Post("/api/auth/reset-password", JsonContent.Create(new
            {
                Token = "invalid-token",
                NewPassword = "NewPassword1!"
            })));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemDetailsAsync(response, 400, ErrorMessages.Auth.ResetPasswordTokenInvalid);
    }

    [Fact]
    public async Task ResetPassword_WeakPassword_Returns400()
    {
        var response = await _client.SendAsync(
            Post("/api/auth/reset-password", JsonContent.Create(new
            {
                Token = "valid-token",
                NewPassword = "weak"
            })));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_MissingToken_Returns400()
    {
        var response = await _client.SendAsync(
            Post("/api/auth/reset-password", JsonContent.Create(new
            {
                NewPassword = "NewPassword1!"
            })));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region VerifyEmail

    [Fact]
    public async Task VerifyEmail_ValidInput_Returns200()
    {
        _factory.AuthenticationService.VerifyEmailAsync(
                Arg.Any<VerifyEmailInput>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var response = await _client.SendAsync(
            Post("/api/auth/verify-email", JsonContent.Create(new
            {
                Token = "valid-token"
            })));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task VerifyEmail_InvalidToken_Returns400WithProblemDetails()
    {
        _factory.AuthenticationService.VerifyEmailAsync(
                Arg.Any<VerifyEmailInput>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(ErrorMessages.Auth.EmailVerificationFailed));

        var response = await _client.SendAsync(
            Post("/api/auth/verify-email", JsonContent.Create(new
            {
                Token = "invalid-token"
            })));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemDetailsAsync(response, 400, ErrorMessages.Auth.EmailVerificationFailed);
    }

    [Fact]
    public async Task VerifyEmail_MissingToken_Returns400()
    {
        var response = await _client.SendAsync(
            Post("/api/auth/verify-email", JsonContent.Create(new { })));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region ResendVerification

    [Fact]
    public async Task ResendVerification_Authenticated_Returns200()
    {
        _factory.AuthenticationService.ResendVerificationEmailAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var response = await _client.SendAsync(
            Post("/api/auth/resend-verification", auth: TestAuth.User()));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ResendVerification_Unauthenticated_Returns401()
    {
        var response = await _client.SendAsync(
            Post("/api/auth/resend-verification"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ResendVerification_AlreadyVerified_Returns400()
    {
        _factory.AuthenticationService.ResendVerificationEmailAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Failure(ErrorMessages.Auth.EmailAlreadyVerified));

        var response = await _client.SendAsync(
            Post("/api/auth/resend-verification", auth: TestAuth.User()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemDetailsAsync(response, 400, ErrorMessages.Auth.EmailAlreadyVerified);
    }

    #endregion

    #region Login_TwoFactor

    [Fact]
    public async Task Login_TwoFactorRequired_Returns200WithChallengeToken()
    {
        _factory.AuthenticationService.Login(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Result<LoginOutput>.Success(new LoginOutput(
                Tokens: null,
                ChallengeToken: "challenge-token-123",
                RequiresTwoFactor: true)));

        var response = await _client.SendAsync(
            Post("/api/auth/login", JsonContent.Create(new { Username = "test@example.com", Password = "Password1!" })));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthTokensResponse>();
        Assert.NotNull(body);
        Assert.True(body.RequiresTwoFactor);
        Assert.Equal("challenge-token-123", body.ChallengeToken);
        Assert.Empty(body.AccessToken);
        Assert.Empty(body.RefreshToken);
    }

    #endregion

    #region TwoFactorLogin

    [Fact]
    public async Task TwoFactorLogin_ValidCode_Returns200()
    {
        _factory.AuthenticationService.CompleteTwoFactorLoginAsync(
                "challenge-token", "123456", false, Arg.Any<CancellationToken>())
            .Returns(Result<AuthenticationOutput>.Success(new AuthenticationOutput("access", "refresh")));

        var response = await _client.SendAsync(
            Post("/api/auth/login/2fa", JsonContent.Create(new { ChallengeToken = "challenge-token", Code = "123456" })));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthTokensResponse>();
        Assert.NotNull(body);
        Assert.NotEmpty(body.AccessToken);
        Assert.NotEmpty(body.RefreshToken);
    }

    [Fact]
    public async Task TwoFactorLogin_InvalidCode_Returns401()
    {
        _factory.AuthenticationService.CompleteTwoFactorLoginAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Result<AuthenticationOutput>.Failure(
                ErrorMessages.TwoFactor.InvalidCode, ErrorType.Unauthorized));

        var response = await _client.SendAsync(
            Post("/api/auth/login/2fa", JsonContent.Create(new { ChallengeToken = "challenge-token", Code = "000000" })));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await AssertProblemDetailsAsync(response, 401, ErrorMessages.TwoFactor.InvalidCode);
    }

    [Fact]
    public async Task TwoFactorLogin_MissingCode_Returns400()
    {
        var response = await _client.SendAsync(
            Post("/api/auth/login/2fa", JsonContent.Create(new { ChallengeToken = "challenge-token", Code = "" })));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task TwoFactorLogin_MissingChallengeToken_Returns400()
    {
        var response = await _client.SendAsync(
            Post("/api/auth/login/2fa", JsonContent.Create(new { ChallengeToken = "", Code = "123456" })));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region TwoFactorRecoveryLogin

    [Fact]
    public async Task TwoFactorRecoveryLogin_ValidCode_Returns200()
    {
        _factory.AuthenticationService.CompleteTwoFactorRecoveryLoginAsync(
                "challenge-token", "RECOV-12345", false, Arg.Any<CancellationToken>())
            .Returns(Result<AuthenticationOutput>.Success(new AuthenticationOutput("access", "refresh")));

        var response = await _client.SendAsync(
            Post("/api/auth/login/2fa/recovery", JsonContent.Create(new { ChallengeToken = "challenge-token", RecoveryCode = "RECOV-12345" })));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthTokensResponse>();
        Assert.NotNull(body);
        Assert.NotEmpty(body.AccessToken);
        Assert.NotEmpty(body.RefreshToken);
    }

    [Fact]
    public async Task TwoFactorRecoveryLogin_InvalidCode_Returns401()
    {
        _factory.AuthenticationService.CompleteTwoFactorRecoveryLoginAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Result<AuthenticationOutput>.Failure(
                ErrorMessages.TwoFactor.RecoveryCodeInvalid, ErrorType.Unauthorized));

        var response = await _client.SendAsync(
            Post("/api/auth/login/2fa/recovery", JsonContent.Create(new { ChallengeToken = "challenge-token", RecoveryCode = "BAD-CODE" })));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await AssertProblemDetailsAsync(response, 401, ErrorMessages.TwoFactor.RecoveryCodeInvalid);
    }

    [Fact]
    public async Task TwoFactorRecoveryLogin_MissingRecoveryCode_Returns400()
    {
        var response = await _client.SendAsync(
            Post("/api/auth/login/2fa/recovery", JsonContent.Create(new { ChallengeToken = "challenge-token", RecoveryCode = "" })));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region TwoFactorSetup

    [Fact]
    public async Task TwoFactorSetup_Authenticated_Returns200()
    {
        _factory.TwoFactorService.SetupAsync(Arg.Any<CancellationToken>())
            .Returns(Result<TwoFactorSetupOutput>.Success(
                new TwoFactorSetupOutput("SHARED-KEY", "otpauth://totp/app:user?secret=SHARED-KEY")));

        var response = await _client.SendAsync(
            Post("/api/auth/2fa/setup", auth: TestAuth.User()));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TwoFactorSetupContract>();
        Assert.NotNull(body);
        Assert.Equal("SHARED-KEY", body.SharedKey);
        Assert.Equal("otpauth://totp/app:user?secret=SHARED-KEY", body.AuthenticatorUri);
    }

    [Fact]
    public async Task TwoFactorSetup_Unauthenticated_Returns401()
    {
        var response = await _client.SendAsync(
            Post("/api/auth/2fa/setup"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await AssertProblemDetailsAsync(response, 401, ErrorMessages.Auth.NotAuthenticated);
    }

    #endregion

    #region TwoFactorVerifySetup

    [Fact]
    public async Task TwoFactorVerifySetup_ValidCode_Returns200()
    {
        _factory.TwoFactorService.VerifySetupAsync("123456", Arg.Any<CancellationToken>())
            .Returns(Result<TwoFactorVerifySetupOutput>.Success(
                new TwoFactorVerifySetupOutput(["CODE1", "CODE2", "CODE3"])));

        var response = await _client.SendAsync(
            Post("/api/auth/2fa/verify-setup",
                JsonContent.Create(new { Code = "123456" }),
                TestAuth.User()));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TwoFactorVerifySetupContract>();
        Assert.NotNull(body);
        Assert.Equal(3, body.RecoveryCodes.Count);
    }

    [Fact]
    public async Task TwoFactorVerifySetup_InvalidCode_Returns400()
    {
        _factory.TwoFactorService.VerifySetupAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<TwoFactorVerifySetupOutput>.Failure(
                ErrorMessages.TwoFactor.VerificationFailed));

        var response = await _client.SendAsync(
            Post("/api/auth/2fa/verify-setup",
                JsonContent.Create(new { Code = "000000" }),
                TestAuth.User()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemDetailsAsync(response, 400, ErrorMessages.TwoFactor.VerificationFailed);
    }

    [Fact]
    public async Task TwoFactorVerifySetup_Unauthenticated_Returns401()
    {
        var response = await _client.SendAsync(
            Post("/api/auth/2fa/verify-setup",
                JsonContent.Create(new { Code = "123456" })));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await AssertProblemDetailsAsync(response, 401, ErrorMessages.Auth.NotAuthenticated);
    }

    #endregion

    #region TwoFactorDisable

    [Fact]
    public async Task TwoFactorDisable_ValidPassword_Returns204()
    {
        _factory.TwoFactorService.DisableAsync("Password1!", Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var response = await _client.SendAsync(
            Post("/api/auth/2fa/disable",
                JsonContent.Create(new { Password = "Password1!" }),
                TestAuth.User()));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task TwoFactorDisable_Unauthenticated_Returns401()
    {
        var response = await _client.SendAsync(
            Post("/api/auth/2fa/disable",
                JsonContent.Create(new { Password = "Password1!" })));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await AssertProblemDetailsAsync(response, 401, ErrorMessages.Auth.NotAuthenticated);
    }

    [Fact]
    public async Task TwoFactorDisable_ServiceFailure_Returns400()
    {
        _factory.TwoFactorService.DisableAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(ErrorMessages.TwoFactor.NotEnabled));

        var response = await _client.SendAsync(
            Post("/api/auth/2fa/disable",
                JsonContent.Create(new { Password = "Password1!" }),
                TestAuth.User()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemDetailsAsync(response, 400, ErrorMessages.TwoFactor.NotEnabled);
    }

    #endregion

    #region TwoFactorRegenerateCodes

    [Fact]
    public async Task TwoFactorRegenerateCodes_ValidPassword_Returns200()
    {
        _factory.TwoFactorService.RegenerateRecoveryCodesAsync("Password1!", Arg.Any<CancellationToken>())
            .Returns(Result<TwoFactorVerifySetupOutput>.Success(
                new TwoFactorVerifySetupOutput(["NEW1", "NEW2", "NEW3"])));

        var response = await _client.SendAsync(
            Post("/api/auth/2fa/recovery-codes",
                JsonContent.Create(new { Password = "Password1!" }),
                TestAuth.User()));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TwoFactorVerifySetupContract>();
        Assert.NotNull(body);
        Assert.Equal(3, body.RecoveryCodes.Count);
    }

    [Fact]
    public async Task TwoFactorRegenerateCodes_Unauthenticated_Returns401()
    {
        var response = await _client.SendAsync(
            Post("/api/auth/2fa/recovery-codes",
                JsonContent.Create(new { Password = "Password1!" })));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await AssertProblemDetailsAsync(response, 401, ErrorMessages.Auth.NotAuthenticated);
    }

    [Fact]
    public async Task TwoFactorRegenerateCodes_ServiceFailure_Returns400()
    {
        _factory.TwoFactorService.RegenerateRecoveryCodesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<TwoFactorVerifySetupOutput>.Failure(ErrorMessages.TwoFactor.NotEnabled));

        var response = await _client.SendAsync(
            Post("/api/auth/2fa/recovery-codes",
                JsonContent.Create(new { Password = "Password1!" }),
                TestAuth.User()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemDetailsAsync(response, 400, ErrorMessages.TwoFactor.NotEnabled);
    }

    #endregion
}
