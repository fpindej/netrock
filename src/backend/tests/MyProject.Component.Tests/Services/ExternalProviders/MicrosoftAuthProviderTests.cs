using System.Net;
using System.Web;
using Microsoft.Extensions.Logging;
using MyProject.Component.Tests.Fixtures;
using MyProject.Infrastructure.Features.Authentication.Services.ExternalProviders;

namespace MyProject.Component.Tests.Services.ExternalProviders;

public class MicrosoftAuthProviderTests
{
    private static readonly ProviderCredentials Credentials = new("test-client-id", "test-client-secret");
    private const string RedirectUri = "https://app.example.com/callback";
    private const string State = "random-state-token";

    [Fact]
    public void Name_ReturnsMicrosoft()
    {
        var (sut, _) = CreateProvider();
        Assert.Equal("Microsoft", sut.Name);
    }

    [Fact]
    public void DisplayName_ReturnsMicrosoft()
    {
        var (sut, _) = CreateProvider();
        Assert.Equal("Microsoft", sut.DisplayName);
    }

    [Fact]
    public void BuildAuthorizationUrl_IncludesRequiredParameters()
    {
        var (sut, _) = CreateProvider();

        var url = sut.BuildAuthorizationUrl(Credentials, State, RedirectUri);

        var uri = new Uri(url);
        var query = HttpUtility.ParseQueryString(uri.Query);

        Assert.Equal("login.microsoftonline.com", uri.Host);
        Assert.Contains("/oauth2/v2.0/authorize", uri.AbsolutePath);
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
    public void BuildAuthorizationUrl_WithoutNonce_OmitsNonce()
    {
        var (sut, _) = CreateProvider();

        var url = sut.BuildAuthorizationUrl(Credentials, State, RedirectUri);

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
                id = "ms-user-123",
                mail = "user@example.com",
                givenName = "John",
                surname = "Doe"
            });
        var (sut, _) = CreateProvider(handler);

        var result = await sut.ExchangeCodeAsync(Credentials, "auth-code", RedirectUri, CancellationToken.None);

        Assert.Equal("ms-user-123", result.ProviderKey);
        Assert.Equal("user@example.com", result.Email);
        Assert.True(result.EmailVerified);
        Assert.Equal("John", result.FirstName);
        Assert.Equal("Doe", result.LastName);
    }

    [Fact]
    public async Task ExchangeCodeAsync_MailNull_FallsBackToUserPrincipalName()
    {
        var handler = new MockHttpMessageHandler()
            .WithJsonResponse(new { access_token = "test-access-token" })
            .WithJsonResponse(new
            {
                id = "ms-user-456",
                mail = (string?)null,
                userPrincipalName = "user@live.com",
                givenName = "Jane",
                surname = "Smith"
            });
        var (sut, _) = CreateProvider(handler);

        var result = await sut.ExchangeCodeAsync(Credentials, "auth-code", RedirectUri, CancellationToken.None);

        Assert.Equal("user@live.com", result.Email);
    }

    [Fact]
    public async Task ExchangeCodeAsync_TokenExchangeFails_Throws()
    {
        var handler = new MockHttpMessageHandler()
            .WithJsonResponse(new { error = "invalid_grant" }, HttpStatusCode.BadRequest);
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
            () => sut.ExchangeCodeAsync(Credentials, "auth-code", RedirectUri, CancellationToken.None));
    }

    [Fact]
    public async Task ExchangeCodeAsync_UserInfoFails_Throws()
    {
        var handler = new MockHttpMessageHandler()
            .WithJsonResponse(new { access_token = "test-token" })
            .WithStatusCode(HttpStatusCode.Forbidden);
        var (sut, _) = CreateProvider(handler);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.ExchangeCodeAsync(Credentials, "auth-code", RedirectUri, CancellationToken.None));
    }

    [Fact]
    public async Task TestConnectionAsync_InvalidClient_ReturnsFailure()
    {
        var handler = new MockHttpMessageHandler()
            .WithJsonResponse(new { error = "invalid_client" });
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
            .WithJsonResponse(new { error = "invalid_grant" });
        var (sut, _) = CreateProvider(handler);

        var result = await sut.TestConnectionAsync(Credentials, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task TestConnectionAsync_HttpException_ReturnsProviderUnreachable()
    {
        var (sut, _) = CreateProvider(new ThrowingHttpMessageHandler());

        var result = await sut.TestConnectionAsync(Credentials, CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task ExchangeCodeAsync_SendsCorrectTokenRequestParameters()
    {
        var handler = new MockHttpMessageHandler()
            .WithJsonResponse(new { access_token = "token" })
            .WithJsonResponse(new { id = "1", mail = "a@b.com" });
        var (sut, _) = CreateProvider(handler);

        await sut.ExchangeCodeAsync(Credentials, "the-code", RedirectUri, CancellationToken.None);

        var body = handler.CapturedBodies[0]!;
        Assert.Contains("code=the-code", body);
        Assert.Contains("client_id=test-client-id", body);
        Assert.Contains("client_secret=test-client-secret", body);
        Assert.Contains("grant_type=authorization_code", body);
    }

    [Fact]
    public async Task ExchangeCodeAsync_UserInfoRequest_UsesBearerToken()
    {
        var handler = new MockHttpMessageHandler()
            .WithJsonResponse(new { access_token = "my-access-token" })
            .WithJsonResponse(new { id = "1", mail = "a@b.com" });
        var (sut, _) = CreateProvider(handler);

        await sut.ExchangeCodeAsync(Credentials, "code", RedirectUri, CancellationToken.None);

        var userInfoRequest = handler.SentRequests[1];
        Assert.Equal("Bearer", userInfoRequest.Headers.Authorization?.Scheme);
        Assert.Equal("my-access-token", userInfoRequest.Headers.Authorization?.Parameter);
    }

    private static (MicrosoftAuthProvider Sut, MockHttpMessageHandler Handler) CreateProvider(
        HttpMessageHandler? handler = null)
    {
        var mockHandler = handler as MockHttpMessageHandler ?? new MockHttpMessageHandler();
        var factory = new MockHttpClientFactory(handler as MockHttpMessageHandler ?? mockHandler);
        var logger = Substitute.For<ILogger<MicrosoftAuthProvider>>();
        var sut = new MicrosoftAuthProvider(factory, logger);

        if (handler is MockHttpMessageHandler mh)
            return (sut, mh);

        // For ThrowingHttpMessageHandler or similar, create a separate factory
        var throwingFactory = new MockHttpClientFactory(mockHandler);
        if (handler is not null and not MockHttpMessageHandler)
        {
            var customFactory = Substitute.For<IHttpClientFactory>();
            customFactory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient(handler));
            sut = new MicrosoftAuthProvider(customFactory, logger);
        }

        return (sut, mockHandler);
    }

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new HttpRequestException("Connection refused");
    }
}
