using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MyProject.Api.Tests.Fixtures;
using MyProject.Shared;

namespace MyProject.Api.Tests.Middlewares;

/// <summary>
/// Integration tests for <see cref="WebApi.Middlewares.OriginValidationMiddleware"/>.
/// <para>
/// The Testing environment configures <c>AllowedOrigins: ["https://test.example.com"]</c>
/// with <c>AllowAllOrigins: false</c>, so only that origin passes validation.
/// </para>
/// </summary>
public class OriginValidationMiddlewareTests : IClassFixture<CustomWebApplicationFactory>, IDisposable
{
    private const string AllowedOrigin = "https://test.example.com";
    private const string DisallowedOrigin = "https://evil.example.com";

    /// <summary>
    /// A public POST endpoint that does not require authentication.
    /// The middleware runs before any controller logic, so the endpoint
    /// itself does not matter for rejection tests.
    /// </summary>
    private const string PublicPostEndpoint = "/api/v1/auth/login";

    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public OriginValidationMiddlewareTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetMocks();
        _client = factory.CreateClient();
    }

    public void Dispose() => _client.Dispose();

    // ── Safe methods ────────────────────────────────────────────────

    [Fact]
    public async Task GetRequest_WithDisallowedOrigin_PassesThrough()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users/me");
        request.Headers.Add("Origin", DisallowedOrigin);

        var response = await _client.SendAsync(request);

        // GET should never be blocked by CSRF middleware — any non-403 proves it passed through
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task HeadRequest_WithDisallowedOrigin_PassesThrough()
    {
        var request = new HttpRequestMessage(HttpMethod.Head, "/api/v1/users/me");
        request.Headers.Add("Origin", DisallowedOrigin);

        var response = await _client.SendAsync(request);

        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OptionsRequest_WithDisallowedOrigin_PassesThrough()
    {
        var request = new HttpRequestMessage(HttpMethod.Options, PublicPostEndpoint);
        request.Headers.Add("Origin", DisallowedOrigin);

        var response = await _client.SendAsync(request);

        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── Missing Origin ──────────────────────────────────────────────

    [Fact]
    public async Task PostRequest_WithoutOriginHeader_PassesThrough()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, PublicPostEndpoint);

        var response = await _client.SendAsync(request);

        // No Origin header means same-origin or non-browser — never blocked
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── Allowed Origin ──────────────────────────────────────────────

    [Fact]
    public async Task PostRequest_WithAllowedOrigin_PassesThrough()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, PublicPostEndpoint);
        request.Headers.Add("Origin", AllowedOrigin);

        var response = await _client.SendAsync(request);

        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── Disallowed Origin — blocked ─────────────────────────────────

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task UnsafeMethod_WithDisallowedOrigin_Returns403(string method)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), PublicPostEndpoint);
        request.Headers.Add("Origin", DisallowedOrigin);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PostRequest_WithDisallowedOrigin_ReturnsProblemDetails()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, PublicPostEndpoint);
        request.Headers.Add("Origin", DisallowedOrigin);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(403, json.GetProperty("status").GetInt32());
        Assert.Equal(ErrorMessages.Security.CrossOriginRequestBlocked, json.GetProperty("detail").GetString());
    }

    // ── Origin case insensitivity ───────────────────────────────────

    [Fact]
    public async Task PostRequest_WithAllowedOriginDifferentCase_PassesThrough()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, PublicPostEndpoint);
        request.Headers.Add("Origin", AllowedOrigin.ToUpperInvariant());

        var response = await _client.SendAsync(request);

        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── Origin variations ───────────────────────────────────────────

    [Theory]
    [InlineData("https://test.example.com.evil.com")]
    [InlineData("https://evil.com")]
    [InlineData("null")]
    public async Task PostRequest_WithVariousDisallowedOrigins_Returns403(string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, PublicPostEndpoint);
        request.Headers.Add("Origin", origin);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
