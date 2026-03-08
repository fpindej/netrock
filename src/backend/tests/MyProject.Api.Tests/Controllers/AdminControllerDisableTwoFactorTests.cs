using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MyProject.Api.Tests.Fixtures;
using MyProject.Application.Identity.Constants;
using MyProject.Shared;

namespace MyProject.Api.Tests.Controllers;

public class AdminControllerDisableTwoFactorTests : IClassFixture<CustomWebApplicationFactory>, IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AdminControllerDisableTwoFactorTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetMocks();
        _client = factory.CreateClient();
    }

    public void Dispose() => _client.Dispose();

    private static HttpRequestMessage Post(string url, string auth, HttpContent? content = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
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

    [Fact]
    public async Task DisableTwoFactor_WithPermission_Returns204()
    {
        var userId = Guid.NewGuid();
        _factory.AdminService.DisableTwoFactorAsync(
                Arg.Any<Guid>(), userId, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var response = await _client.SendAsync(
            Post($"/api/v1/admin/users/{userId}/disable-2fa",
                TestAuth.WithPermissions(AppPermissions.Users.ManageTwoFactor),
                JsonContent.Create(new { Reason = "Lost device" })));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DisableTwoFactor_WithoutPermission_Returns403()
    {
        var response = await _client.SendAsync(
            Post($"/api/v1/admin/users/{Guid.NewGuid()}/disable-2fa",
                TestAuth.User(),
                JsonContent.Create(new { Reason = (string?)null })));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DisableTwoFactor_Unauthenticated_Returns401()
    {
        using var anonClient = _factory.CreateClient();

        var response = await anonClient.PostAsJsonAsync(
            $"/api/v1/admin/users/{Guid.NewGuid()}/disable-2fa",
            new { Reason = (string?)null });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DisableTwoFactor_NotFound_Returns404()
    {
        var userId = Guid.NewGuid();
        _factory.AdminService.DisableTwoFactorAsync(
                Arg.Any<Guid>(), userId, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(ErrorMessages.Admin.UserNotFound, ErrorType.NotFound));

        var response = await _client.SendAsync(
            Post($"/api/v1/admin/users/{userId}/disable-2fa",
                TestAuth.WithPermissions(AppPermissions.Users.ManageTwoFactor),
                JsonContent.Create(new { Reason = (string?)null })));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertProblemDetailsAsync(response, 404, ErrorMessages.Admin.UserNotFound);
    }

    [Fact]
    public async Task DisableTwoFactor_TwoFactorNotEnabled_Returns400()
    {
        var userId = Guid.NewGuid();
        _factory.AdminService.DisableTwoFactorAsync(
                Arg.Any<Guid>(), userId, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(ErrorMessages.Admin.TwoFactorNotEnabled));

        var response = await _client.SendAsync(
            Post($"/api/v1/admin/users/{userId}/disable-2fa",
                TestAuth.WithPermissions(AppPermissions.Users.ManageTwoFactor),
                JsonContent.Create(new { Reason = (string?)null })));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemDetailsAsync(response, 400, ErrorMessages.Admin.TwoFactorNotEnabled);
    }

    [Fact]
    public async Task DisableTwoFactor_ReasonTooLong_Returns400()
    {
        var response = await _client.SendAsync(
            Post($"/api/v1/admin/users/{Guid.NewGuid()}/disable-2fa",
                TestAuth.WithPermissions(AppPermissions.Users.ManageTwoFactor),
                JsonContent.Create(new { Reason = new string('a', 501) })));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DisableTwoFactor_NullReason_Returns204()
    {
        var userId = Guid.NewGuid();
        _factory.AdminService.DisableTwoFactorAsync(
                Arg.Any<Guid>(), userId, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var response = await _client.SendAsync(
            Post($"/api/v1/admin/users/{userId}/disable-2fa",
                TestAuth.WithPermissions(AppPermissions.Users.ManageTwoFactor),
                JsonContent.Create(new { Reason = (string?)null })));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DisableTwoFactor_UsersManagePermission_Returns403()
    {
        // users.manage alone should NOT grant access - needs users.manage_2fa
        var response = await _client.SendAsync(
            Post($"/api/v1/admin/users/{Guid.NewGuid()}/disable-2fa",
                TestAuth.WithPermissions(AppPermissions.Users.Manage),
                JsonContent.Create(new { Reason = (string?)null })));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DisableTwoFactor_Superuser_Returns204()
    {
        var userId = Guid.NewGuid();
        _factory.AdminService.DisableTwoFactorAsync(
                Arg.Any<Guid>(), userId, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var response = await _client.SendAsync(
            Post($"/api/v1/admin/users/{userId}/disable-2fa",
                TestAuth.Superuser(),
                JsonContent.Create(new { Reason = (string?)null })));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
