using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MyProject.Api.Tests.Contracts;
using MyProject.Api.Tests.Fixtures;
using MyProject.Application.Features.Authentication.Dtos;
using MyProject.Application.Identity.Constants;

namespace MyProject.Api.Tests.Controllers;

public class OAuthProvidersControllerTests : IClassFixture<CustomWebApplicationFactory>, IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public OAuthProvidersControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetMocks();
        _client = factory.CreateClient();
    }

    public void Dispose() => _client.Dispose();

    private static HttpRequestMessage Get(string url, string auth)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("Authorization", auth);
        return request;
    }

    private static HttpRequestMessage Put(string url, string auth, HttpContent? content = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, url) { Content = content };
        request.Headers.TryAddWithoutValidation("Authorization", auth);
        return request;
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

    #region ListProviders

    [Fact]
    public async Task ListProviders_WithPermission_ReturnsProviders()
    {
        _factory.ProviderConfigService.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ProviderConfigOutput>
            {
                new("Google", "Google", true, "google-client-id", true, "database", DateTime.UtcNow, Guid.NewGuid()),
                new("GitHub", "GitHub", false, null, false, "appsettings", null, null)
            });

        var response = await _client.SendAsync(
            Get("/api/v1/admin/oauth-providers",
                TestAuth.WithPermissions(AppPermissions.OAuthProviders.View)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<OAuthProviderConfigContract>>();
        Assert.NotNull(body);
        Assert.Equal(2, body.Count);
        Assert.Equal("Google", body[0].Provider);
        Assert.True(body[0].IsEnabled);
        Assert.Equal("GitHub", body[1].Provider);
        Assert.False(body[1].IsEnabled);
    }

    [Fact]
    public async Task ListProviders_Unauthenticated_Returns401()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/oauth-providers");
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ListProviders_NoPermission_Returns403()
    {
        var response = await _client.SendAsync(
            Get("/api/v1/admin/oauth-providers", TestAuth.User()));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region UpdateProvider

    [Fact]
    public async Task UpdateProvider_ValidRequest_Returns204()
    {
        _factory.ProviderConfigService.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ProviderConfigOutput>
            {
                new("Google", "Google", false, null, false, "appsettings", null, null)
            });

        var response = await _client.SendAsync(
            Put("/api/v1/admin/oauth-providers/Google",
                TestAuth.WithPermissions(AppPermissions.OAuthProviders.Manage),
                JsonContent.Create(new { IsEnabled = true, ClientId = "new-id", ClientSecret = "new-secret" })));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await _factory.ProviderConfigService.Received(1).UpsertAsync(
            Arg.Any<Guid>(), Arg.Any<UpsertProviderConfigInput>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateProvider_UnknownProvider_Returns400()
    {
        _factory.ProviderConfigService.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ProviderConfigOutput>
            {
                new("Google", "Google", false, null, false, "appsettings", null, null)
            });

        var response = await _client.SendAsync(
            Put("/api/v1/admin/oauth-providers/Unknown",
                TestAuth.WithPermissions(AppPermissions.OAuthProviders.Manage),
                JsonContent.Create(new { IsEnabled = true, ClientId = "id", ClientSecret = "secret" })));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProvider_EnabledWithoutClientId_Returns400()
    {
        var response = await _client.SendAsync(
            Put("/api/v1/admin/oauth-providers/Google",
                TestAuth.WithPermissions(AppPermissions.OAuthProviders.Manage),
                JsonContent.Create(new { IsEnabled = true, ClientId = "", ClientSecret = "secret" })));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProvider_Unauthenticated_Returns401()
    {
        var request = new HttpRequestMessage(HttpMethod.Put, "/api/v1/admin/oauth-providers/Google")
        {
            Content = JsonContent.Create(new { IsEnabled = true, ClientId = "id", ClientSecret = "secret" })
        };

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProvider_NoPermission_Returns403()
    {
        var response = await _client.SendAsync(
            Put("/api/v1/admin/oauth-providers/Google",
                TestAuth.User(),
                JsonContent.Create(new { IsEnabled = true, ClientId = "id", ClientSecret = "secret" })));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProvider_InvalidProviderNameFormat_Returns404()
    {
        var response = await _client.SendAsync(
            Put("/api/v1/admin/oauth-providers/invalid-name!",
                TestAuth.WithPermissions(AppPermissions.OAuthProviders.Manage),
                JsonContent.Create(new { IsEnabled = true, ClientId = "id", ClientSecret = "secret" })));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion
}
