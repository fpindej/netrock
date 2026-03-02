using System.Net;
using System.Web;
using Microsoft.Extensions.Logging;
using MyProject.Component.Tests.Fixtures;
using MyProject.Infrastructure.Features.Authentication.Services.ExternalProviders;

namespace MyProject.Component.Tests.Services.ExternalProviders;

public class SlackAuthProviderTests
{
    private static readonly ProviderCredentials Credentials = new("test-client-id", "test-client-secret");
    private const string RedirectUri = "https://app.example.com/callback";
    private const string State = "random-state-token";

    [Fact]
    public void Name_ReturnsSlack()
    {
        var (sut, _) = CreateProvider();
        Assert.Equal("Slack", sut.Name);
    }

    [Fact]
    public void DisplayName_ReturnsSlack()
    {
        var (sut, _) = CreateProvider();
        Assert.Equal("Slack", sut.DisplayName);
    }

    [Fact]
    public void BuildAuthorizationUrl_IncludesRequiredParameters()
    {
        var (sut, _) = CreateProvider();

        var url = sut.BuildAuthorizationUrl(Credentials, State, RedirectUri);

        var uri = new Uri(url);
        var query = HttpUtility.ParseQueryString(uri.Query);

        Assert.Equal("slack.com", uri.Host);
        Assert.Contains("/openid/connect/authorize", uri.AbsolutePath);
        Assert.Equal("test-client-id", query["client_id"]);
        Assert.Equal(RedirectUri, query["redirect_uri"]);
        Assert.Equal("code", query["response_type"]);
        Assert.Equal("openid email profile", query["scope"]);
        Assert.Equal(State, query["state"]);
    }

    [Fact]
    public void BuildAuthorizationUrl_WithNonce_IncludesNonce()
    {
        var (sut, _) = CreateProvider();

        var url = sut.BuildAuthorizationUrl(Credentials, State, RedirectUri, nonce: "test-nonce");

        var query = HttpUtility.ParseQueryString(new Uri(url).Query);
        Assert.Equal("test-nonce", query["nonce"]);
    }

    [Fact]
    public async Task ExchangeCodeAsync_ValidResponse_ReturnsUserInfo()
    {
        var handler = new MockHttpMessageHandler()
            .WithJsonResponse(new { ok = true, access_token = "test-access-token" })
            .WithJsonResponse(new
            {
                ok = true,
                sub = "slack-user-123",
                email = "user@example.com",
                email_verified = true,
                given_name = "John",
                family_name = "Doe"
            });
        var (sut, _) = CreateProvider(handler);

        var result = await sut.ExchangeCodeAsync(Credentials, "auth-code", RedirectUri, CancellationToken.None);

        Assert.Equal("slack-user-123", result.ProviderKey);
        Assert.Equal("user@example.com", result.Email);
        Assert.True(result.EmailVerified);
        Assert.Equal("John", result.FirstName);
        Assert.Equal("Doe", result.LastName);
    }

    [Fact]
    public async Task ExchangeCodeAsync_TokenNotOk_Throws()
    {
        var handler = new MockHttpMessageHandler()
            .WithJsonResponse(new { ok = false, error = "invalid_code" });
        var (sut, _) = CreateProvider(handler);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.ExchangeCodeAsync(Credentials, "bad-code", RedirectUri, CancellationToken.None));
    }

    [Fact]
    public async Task ExchangeCodeAsync_UserInfoNotOk_Throws()
    {
        var handler = new MockHttpMessageHandler()
            .WithJsonResponse(new { ok = true, access_token = "token" })
            .WithJsonResponse(new { ok = false, error = "token_revoked" });
        var (sut, _) = CreateProvider(handler);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.ExchangeCodeAsync(Credentials, "code", RedirectUri, CancellationToken.None));
    }

    [Fact]
    public async Task ExchangeCodeAsync_EmptyAccessToken_Throws()
    {
        var handler = new MockHttpMessageHandler()
            .WithJsonResponse(new { ok = true, access_token = "" });
        var (sut, _) = CreateProvider(handler);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.ExchangeCodeAsync(Credentials, "code", RedirectUri, CancellationToken.None));
    }

    [Fact]
    public async Task ExchangeCodeAsync_TokenHttpFailure_Throws()
    {
        var handler = new MockHttpMessageHandler()
            .WithStatusCode(HttpStatusCode.InternalServerError);
        var (sut, _) = CreateProvider(handler);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.ExchangeCodeAsync(Credentials, "code", RedirectUri, CancellationToken.None));
    }

    [Fact]
    public async Task ExchangeCodeAsync_MissingSub_Throws()
    {
        var handler = new MockHttpMessageHandler()
            .WithJsonResponse(new { ok = true, access_token = "token" })
            .WithJsonResponse(new { ok = true, email = "a@b.com", email_verified = true });
        var (sut, _) = CreateProvider(handler);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.ExchangeCodeAsync(Credentials, "code", RedirectUri, CancellationToken.None));
    }

    [Fact]
    public async Task TestConnectionAsync_InvalidClientId_ReturnsFailure()
    {
        var handler = new MockHttpMessageHandler()
            .WithJsonResponse(new { ok = false, error = "invalid_client_id" });
        var (sut, _) = CreateProvider(handler);

        var result = await sut.TestConnectionAsync(Credentials, CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task TestConnectionAsync_InvalidCode_ReturnsSuccess()
    {
        var handler = new MockHttpMessageHandler()
            .WithJsonResponse(new { ok = false, error = "invalid_code" });
        var (sut, _) = CreateProvider(handler);

        var result = await sut.TestConnectionAsync(Credentials, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task TestConnectionAsync_Unauthorized_ReturnsFailure()
    {
        var handler = new MockHttpMessageHandler()
            .WithStatusCode(HttpStatusCode.Unauthorized);
        var (sut, _) = CreateProvider(handler);

        var result = await sut.TestConnectionAsync(Credentials, CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task TestConnectionAsync_HttpException_ReturnsProviderUnreachable()
    {
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient(new ThrowingHandler()));
        var sut = new SlackAuthProvider(factory, Substitute.For<ILogger<SlackAuthProvider>>());

        var result = await sut.TestConnectionAsync(Credentials, CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task ExchangeCodeAsync_DoesNotSendGrantType()
    {
        var handler = new MockHttpMessageHandler()
            .WithJsonResponse(new { ok = true, access_token = "token" })
            .WithJsonResponse(new { ok = true, sub = "u", email = "a@b.com" });
        var (sut, _) = CreateProvider(handler);

        await sut.ExchangeCodeAsync(Credentials, "code", RedirectUri, CancellationToken.None);

        var body = handler.CapturedBodies[0]!;
        Assert.DoesNotContain("grant_type", body);
    }

    private static (SlackAuthProvider Sut, MockHttpMessageHandler Handler) CreateProvider(
        MockHttpMessageHandler? handler = null)
    {
        handler ??= new MockHttpMessageHandler();
        var factory = new MockHttpClientFactory(handler);
        var logger = Substitute.For<ILogger<SlackAuthProvider>>();
        return (new SlackAuthProvider(factory, logger), handler);
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new HttpRequestException("Connection refused");
    }
}
