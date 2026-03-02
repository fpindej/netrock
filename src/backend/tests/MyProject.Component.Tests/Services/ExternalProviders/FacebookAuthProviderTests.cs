using System.Net;
using System.Web;
using Microsoft.Extensions.Logging;
using MyProject.Component.Tests.Fixtures;
using MyProject.Infrastructure.Features.Authentication.Services.ExternalProviders;

namespace MyProject.Component.Tests.Services.ExternalProviders;

public class FacebookAuthProviderTests
{
    private static readonly ProviderCredentials Credentials = new("test-client-id", "test-client-secret");
    private const string RedirectUri = "https://app.example.com/callback";
    private const string State = "random-state-token";

    [Fact]
    public void Name_ReturnsFacebook()
    {
        var (sut, _) = CreateProvider();
        Assert.Equal("Facebook", sut.Name);
    }

    [Fact]
    public void DisplayName_ReturnsFacebook()
    {
        var (sut, _) = CreateProvider();
        Assert.Equal("Facebook", sut.DisplayName);
    }

    [Fact]
    public void BuildAuthorizationUrl_IncludesRequiredParameters()
    {
        var (sut, _) = CreateProvider();

        var url = sut.BuildAuthorizationUrl(Credentials, State, RedirectUri);

        var uri = new Uri(url);
        var query = HttpUtility.ParseQueryString(uri.Query);

        Assert.Equal("www.facebook.com", uri.Host);
        Assert.Contains("/dialog/oauth", uri.AbsolutePath);
        Assert.Equal("test-client-id", query["client_id"]);
        Assert.Equal(RedirectUri, query["redirect_uri"]);
        Assert.Equal("code", query["response_type"]);
        Assert.Equal("email public_profile", query["scope"]);
        Assert.Equal(State, query["state"]);
    }

    [Fact]
    public void BuildAuthorizationUrl_DoesNotIncludeNonce()
    {
        var (sut, _) = CreateProvider();

        var url = sut.BuildAuthorizationUrl(Credentials, State, RedirectUri, nonce: "ignored");

        var query = HttpUtility.ParseQueryString(new Uri(url).Query);
        Assert.Null(query["nonce"]);
    }

    [Fact]
    public async Task ExchangeCodeAsync_ValidResponse_ReturnsUserInfo()
    {
        var handler = new MockHttpMessageHandler()
            .WithJsonResponse(new { access_token = "test-access-token" })
            .WithJsonResponse(new
            {
                id = "fb-user-123",
                email = "user@example.com",
                first_name = "John",
                last_name = "Doe"
            });
        var (sut, _) = CreateProvider(handler);

        var result = await sut.ExchangeCodeAsync(Credentials, "auth-code", RedirectUri, CancellationToken.None);

        Assert.Equal("fb-user-123", result.ProviderKey);
        Assert.Equal("user@example.com", result.Email);
        Assert.True(result.EmailVerified);
        Assert.Equal("John", result.FirstName);
        Assert.Equal("Doe", result.LastName);
    }

    [Fact]
    public async Task ExchangeCodeAsync_AlwaysSetsEmailVerifiedTrue()
    {
        var handler = new MockHttpMessageHandler()
            .WithJsonResponse(new { access_token = "token" })
            .WithJsonResponse(new
            {
                id = "fb-user",
                email = "user@example.com",
                first_name = "Test"
            });
        var (sut, _) = CreateProvider(handler);

        var result = await sut.ExchangeCodeAsync(Credentials, "code", RedirectUri, CancellationToken.None);

        Assert.True(result.EmailVerified);
    }

    [Fact]
    public async Task ExchangeCodeAsync_TokenExchangeFails_Throws()
    {
        var handler = new MockHttpMessageHandler()
            .WithJsonResponse(new { error = new { message = "bad", type = "OAuthException", code = 190 } }, HttpStatusCode.BadRequest);
        var (sut, _) = CreateProvider(handler);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.ExchangeCodeAsync(Credentials, "bad-code", RedirectUri, CancellationToken.None));
    }

    [Fact]
    public async Task ExchangeCodeAsync_EmptyAccessToken_Throws()
    {
        var handler = new MockHttpMessageHandler()
            .WithJsonResponse(new { access_token = "" });
        var (sut, _) = CreateProvider(handler);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.ExchangeCodeAsync(Credentials, "code", RedirectUri, CancellationToken.None));
    }

    [Fact]
    public async Task ExchangeCodeAsync_MissingId_Throws()
    {
        var handler = new MockHttpMessageHandler()
            .WithJsonResponse(new { access_token = "token" })
            .WithJsonResponse(new { email = "a@b.com" });
        var (sut, _) = CreateProvider(handler);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.ExchangeCodeAsync(Credentials, "code", RedirectUri, CancellationToken.None));
    }

    [Fact]
    public async Task ExchangeCodeAsync_MissingEmail_Throws()
    {
        var handler = new MockHttpMessageHandler()
            .WithJsonResponse(new { access_token = "token" })
            .WithJsonResponse(new { id = "123" });
        var (sut, _) = CreateProvider(handler);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.ExchangeCodeAsync(Credentials, "code", RedirectUri, CancellationToken.None));
    }

    [Fact]
    public async Task ExchangeCodeAsync_DoesNotSendGrantType()
    {
        var handler = new MockHttpMessageHandler()
            .WithJsonResponse(new { access_token = "token" })
            .WithJsonResponse(new { id = "1", email = "a@b.com" });
        var (sut, _) = CreateProvider(handler);

        await sut.ExchangeCodeAsync(Credentials, "code", RedirectUri, CancellationToken.None);

        var body = handler.CapturedBodies[0]!;
        Assert.DoesNotContain("grant_type", body);
    }

    [Fact]
    public async Task TestConnectionAsync_OAuthException_ReturnsFailure()
    {
        var handler = new MockHttpMessageHandler()
            .WithJsonResponse(new { error = new { type = "OAuthException", code = 101 } });
        var (sut, _) = CreateProvider(handler);

        var result = await sut.TestConnectionAsync(Credentials, CancellationToken.None);

        Assert.True(result.IsFailure);
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
    public async Task TestConnectionAsync_InvalidCode_ReturnsSuccess()
    {
        var handler = new MockHttpMessageHandler()
            .WithJsonResponse(new { error = new { type = "OAuthException", code = 100 } });
        var (sut, _) = CreateProvider(handler);

        var result = await sut.TestConnectionAsync(Credentials, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task TestConnectionAsync_HttpException_ReturnsProviderUnreachable()
    {
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient(new ThrowingHandler()));
        var sut = new FacebookAuthProvider(factory, Substitute.For<ILogger<FacebookAuthProvider>>());

        var result = await sut.TestConnectionAsync(Credentials, CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    private static (FacebookAuthProvider Sut, MockHttpMessageHandler Handler) CreateProvider(
        MockHttpMessageHandler? handler = null)
    {
        handler ??= new MockHttpMessageHandler();
        var factory = new MockHttpClientFactory(handler);
        var logger = Substitute.For<ILogger<FacebookAuthProvider>>();
        return (new FacebookAuthProvider(factory, logger), handler);
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new HttpRequestException("Connection refused");
    }
}
