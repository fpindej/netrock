using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MyProject.Api.Tests.Contracts;
using MyProject.Api.Tests.Fixtures;
using MyProject.Application.Features.Authentication.Dtos;
using MyProject.Shared;

namespace MyProject.Api.Tests.Controllers;

public class ExternalAuthControllerTests : IClassFixture<CustomWebApplicationFactory>, IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ExternalAuthControllerTests(CustomWebApplicationFactory factory)
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

    private static HttpRequestMessage Get(string url, string? auth = null)
    {
        var msg = new HttpRequestMessage(HttpMethod.Get, url);
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

    #region GetProviders

    [Fact]
    public async Task GetProviders_ReturnsProviders()
    {
        _factory.ExternalAuthService.GetAvailableProviders()
            .Returns(new List<ExternalProviderInfo>
            {
                new("Google", "Google"),
                new("GitHub", "GitHub")
            });

        var response = await _client.SendAsync(Get("/api/auth/providers"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<ExternalProviderContract>>();
        Assert.NotNull(body);
        Assert.Equal(2, body.Count);
        Assert.Equal("Google", body[0].Name);
        Assert.Equal("GitHub", body[1].Name);
    }

    [Fact]
    public async Task GetProviders_NoProviders_ReturnsEmptyList()
    {
        _factory.ExternalAuthService.GetAvailableProviders()
            .Returns(new List<ExternalProviderInfo>());

        var response = await _client.SendAsync(Get("/api/auth/providers"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<ExternalProviderContract>>();
        Assert.NotNull(body);
        Assert.Empty(body);
    }

    #endregion

    #region ExternalChallenge

    [Fact]
    public async Task ExternalChallenge_ValidRequest_Returns200WithAuthUrl()
    {
        _factory.ExternalAuthService.CreateChallengeAsync(
                Arg.Any<ExternalChallengeInput>(), Arg.Any<CancellationToken>())
            .Returns(Result<ExternalChallengeOutput>.Success(
                new ExternalChallengeOutput("https://accounts.google.com/o/oauth2/v2/auth?state=abc")));

        var response = await _client.SendAsync(
            Post("/api/auth/external/challenge",
                JsonContent.Create(new { Provider = "Google", RedirectUri = "https://example.com/oauth/callback" })));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ExternalChallengeContract>();
        Assert.NotNull(body);
        Assert.StartsWith("https://accounts.google.com", body.AuthorizationUrl);
    }

    [Fact]
    public async Task ExternalChallenge_InvalidProvider_Returns400()
    {
        _factory.ExternalAuthService.CreateChallengeAsync(
                Arg.Any<ExternalChallengeInput>(), Arg.Any<CancellationToken>())
            .Returns(Result<ExternalChallengeOutput>.Failure(
                ErrorMessages.ExternalAuth.ProviderNotConfigured, ErrorType.Validation));

        var response = await _client.SendAsync(
            Post("/api/auth/external/challenge",
                JsonContent.Create(new { Provider = "Unknown", RedirectUri = "https://example.com/oauth/callback" })));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemDetailsAsync(response, 400, ErrorMessages.ExternalAuth.ProviderNotConfigured);
    }

    [Fact]
    public async Task ExternalChallenge_MissingProvider_Returns400()
    {
        var response = await _client.SendAsync(
            Post("/api/auth/external/challenge",
                JsonContent.Create(new { Provider = "", RedirectUri = "https://example.com/oauth/callback" })));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ExternalChallenge_InvalidRedirectUri_Returns400()
    {
        var response = await _client.SendAsync(
            Post("/api/auth/external/challenge",
                JsonContent.Create(new { Provider = "Google", RedirectUri = "not-a-uri" })));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region ExternalCallback

    [Fact]
    public async Task ExternalCallback_ValidCode_Returns200WithTokens()
    {
        _factory.ExternalAuthService.HandleCallbackAsync(
                Arg.Any<ExternalCallbackInput>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Result<ExternalCallbackOutput>.Success(
                new ExternalCallbackOutput(
                    new AuthenticationOutput("access-token", "refresh-token"),
                    IsNewUser: false, Provider: "Google", IsLinkOnly: false)));

        var response = await _client.SendAsync(
            Post("/api/auth/external/callback",
                JsonContent.Create(new { Code = "auth-code", State = "state-token" })));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ExternalCallbackContract>();
        Assert.NotNull(body);
        Assert.Equal("access-token", body.AccessToken);
        Assert.Equal("refresh-token", body.RefreshToken);
        Assert.False(body.IsNewUser);
        Assert.Equal("Google", body.Provider);
        Assert.False(body.IsLinkOnly);
    }

    [Fact]
    public async Task ExternalCallback_NewUser_Returns200WithIsNewUser()
    {
        _factory.ExternalAuthService.HandleCallbackAsync(
                Arg.Any<ExternalCallbackInput>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Result<ExternalCallbackOutput>.Success(
                new ExternalCallbackOutput(
                    new AuthenticationOutput("access-token", "refresh-token"),
                    IsNewUser: true, Provider: "GitHub", IsLinkOnly: false)));

        var response = await _client.SendAsync(
            Post("/api/auth/external/callback",
                JsonContent.Create(new { Code = "auth-code", State = "state-token" })));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ExternalCallbackContract>();
        Assert.NotNull(body);
        Assert.True(body.IsNewUser);
    }

    [Fact]
    public async Task ExternalCallback_LinkOnly_Returns200()
    {
        _factory.ExternalAuthService.HandleCallbackAsync(
                Arg.Any<ExternalCallbackInput>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Result<ExternalCallbackOutput>.Success(
                new ExternalCallbackOutput(
                    Tokens: null, IsNewUser: false, Provider: "Google", IsLinkOnly: true)));

        var response = await _client.SendAsync(
            Post("/api/auth/external/callback",
                JsonContent.Create(new { Code = "auth-code", State = "state-token" })));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ExternalCallbackContract>();
        Assert.NotNull(body);
        Assert.True(body.IsLinkOnly);
    }

    [Fact]
    public async Task ExternalCallback_InvalidState_Returns400()
    {
        _factory.ExternalAuthService.HandleCallbackAsync(
                Arg.Any<ExternalCallbackInput>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Result<ExternalCallbackOutput>.Failure(
                ErrorMessages.ExternalAuth.InvalidState, ErrorType.Validation));

        var response = await _client.SendAsync(
            Post("/api/auth/external/callback",
                JsonContent.Create(new { Code = "auth-code", State = "bad-state" })));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemDetailsAsync(response, 400, ErrorMessages.ExternalAuth.InvalidState);
    }

    [Fact]
    public async Task ExternalCallback_MissingCode_Returns400()
    {
        var response = await _client.SendAsync(
            Post("/api/auth/external/callback",
                JsonContent.Create(new { Code = "", State = "state-token" })));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region ExternalUnlink

    [Fact]
    public async Task ExternalUnlink_ValidProvider_Returns204()
    {
        _factory.ExternalAuthService.UnlinkProviderAsync(
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var response = await _client.SendAsync(
            Post("/api/auth/external/unlink",
                JsonContent.Create(new { Provider = "Google" }),
                TestAuth.User()));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ExternalUnlink_LastAuthMethod_Returns400()
    {
        _factory.ExternalAuthService.UnlinkProviderAsync(
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(
                ErrorMessages.ExternalAuth.CannotUnlinkLastMethod, ErrorType.Validation));

        var response = await _client.SendAsync(
            Post("/api/auth/external/unlink",
                JsonContent.Create(new { Provider = "Google" }),
                TestAuth.User()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemDetailsAsync(response, 400, ErrorMessages.ExternalAuth.CannotUnlinkLastMethod);
    }

    [Fact]
    public async Task ExternalUnlink_Unauthenticated_Returns401()
    {
        var response = await _client.SendAsync(
            Post("/api/auth/external/unlink",
                JsonContent.Create(new { Provider = "Google" })));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region SetPassword

    [Fact]
    public async Task SetPassword_ValidPassword_Returns204()
    {
        _factory.ExternalAuthService.SetPasswordAsync(
                Arg.Any<SetPasswordInput>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var response = await _client.SendAsync(
            Post("/api/auth/set-password",
                JsonContent.Create(new { NewPassword = "Password1!" }),
                TestAuth.User()));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task SetPassword_AlreadyHasPassword_Returns400()
    {
        _factory.ExternalAuthService.SetPasswordAsync(
                Arg.Any<SetPasswordInput>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(
                ErrorMessages.ExternalAuth.PasswordAlreadySet, ErrorType.Validation));

        var response = await _client.SendAsync(
            Post("/api/auth/set-password",
                JsonContent.Create(new { NewPassword = "Password1!" }),
                TestAuth.User()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemDetailsAsync(response, 400, ErrorMessages.ExternalAuth.PasswordAlreadySet);
    }

    [Fact]
    public async Task SetPassword_Unauthenticated_Returns401()
    {
        var response = await _client.SendAsync(
            Post("/api/auth/set-password",
                JsonContent.Create(new { NewPassword = "Password1!" })));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SetPassword_InvalidPassword_Returns400()
    {
        var response = await _client.SendAsync(
            Post("/api/auth/set-password",
                JsonContent.Create(new { NewPassword = "" }),
                TestAuth.User()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion
}
