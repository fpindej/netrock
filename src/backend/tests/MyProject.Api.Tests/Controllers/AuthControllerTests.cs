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
            .Returns(Result<AuthenticationOutput>.Success(new AuthenticationOutput("access", "refresh")));

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
            .Returns(Result<AuthenticationOutput>.Failure(
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
        _factory.AuthenticationService.Register(Arg.Any<RegisterInput>(), Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        var response = await _client.SendAsync(
            Post("/api/auth/register", JsonContent.Create(new { Email = "new@example.com", Password = "Password1!" })));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RegisterUserResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.Id);
    }

    [Fact]
    public async Task Register_InvalidEmail_Returns400()
    {
        var response = await _client.SendAsync(
            Post("/api/auth/register", JsonContent.Create(new { Email = "not-an-email", Password = "Password1!" })));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WeakPassword_Returns400()
    {
        var response = await _client.SendAsync(
            Post("/api/auth/register", JsonContent.Create(new { Email = "test@example.com", Password = "weak" })));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_ServiceFailure_Returns400WithProblemDetails()
    {
        _factory.AuthenticationService.Register(Arg.Any<RegisterInput>(), Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Failure("Email already registered."));

        var response = await _client.SendAsync(
            Post("/api/auth/register", JsonContent.Create(new { Email = "dup@example.com", Password = "Password1!" })));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemDetailsAsync(response, 400, "Email already registered.");
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
        _factory.AuthenticationService.ForgotPasswordAsync(
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var response = await _client.SendAsync(
            Post("/api/auth/forgot-password", JsonContent.Create(new { Email = "test@example.com" })));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_InvalidEmail_Returns400()
    {
        var response = await _client.SendAsync(
            Post("/api/auth/forgot-password", JsonContent.Create(new { Email = "not-an-email" })));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_EmptyEmail_Returns400()
    {
        var response = await _client.SendAsync(
            Post("/api/auth/forgot-password", JsonContent.Create(new { Email = "" })));

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
                Email = "test@example.com",
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
                Email = "test@example.com",
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
                Email = "test@example.com",
                Token = "valid-token",
                NewPassword = "weak"
            })));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_MissingEmail_Returns400()
    {
        var response = await _client.SendAsync(
            Post("/api/auth/reset-password", JsonContent.Create(new
            {
                Token = "valid-token",
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
                Email = "test@example.com",
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
                Email = "test@example.com",
                Token = "invalid-token"
            })));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemDetailsAsync(response, 400, ErrorMessages.Auth.EmailVerificationFailed);
    }

    [Fact]
    public async Task VerifyEmail_InvalidEmail_Returns400()
    {
        var response = await _client.SendAsync(
            Post("/api/auth/verify-email", JsonContent.Create(new
            {
                Email = "not-an-email",
                Token = "valid-token"
            })));

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
}
