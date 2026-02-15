using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MyProject.Api.Tests.Contracts;
using MyProject.Api.Tests.Fixtures;
using MyProject.Application.Features.Authentication.Dtos;
using MyProject.Application.Identity.Dtos;
using MyProject.Shared;

namespace MyProject.Api.Tests.Controllers;

public class UsersControllerTests : IClassFixture<CustomWebApplicationFactory>, IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UsersControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetMocks();
        _client = factory.CreateClient();
    }

    public void Dispose() => _client.Dispose();

    private static HttpRequestMessage Get(string url, string? auth = null)
    {
        var msg = new HttpRequestMessage(HttpMethod.Get, url);
        if (auth is not null) msg.Headers.Add("Authorization", auth);
        return msg;
    }

    private static HttpRequestMessage Patch(string url, HttpContent content, string? auth = null)
    {
        var msg = new HttpRequestMessage(HttpMethod.Patch, url) { Content = content };
        if (auth is not null) msg.Headers.Add("Authorization", auth);
        return msg;
    }

    private static HttpRequestMessage Delete(string url, HttpContent? content, string? auth = null)
    {
        var msg = new HttpRequestMessage(HttpMethod.Delete, url) { Content = content };
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

    #region GetMe

    [Fact]
    public async Task GetMe_Authenticated_Returns200()
    {
        _factory.UserService.GetCurrentUserAsync(Arg.Any<CancellationToken>())
            .Returns(Result<UserOutput>.Success(new UserOutput(
                Guid.NewGuid(), "test@example.com", "John", "Doe",
                null, null, null, ["User"], [])));

        var response = await _client.SendAsync(Get("/api/users/me", TestAuth.User()));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UserMeResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.Id);
        Assert.Equal("test@example.com", body.Username);
        Assert.Equal("John", body.FirstName);
        Assert.Contains("User", body.Roles);
    }

    [Fact]
    public async Task GetMe_Unauthenticated_Returns401()
    {
        var response = await _client.SendAsync(Get("/api/users/me"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await AssertProblemDetailsAsync(response, 401, ErrorMessages.Auth.NotAuthenticated);
    }

    [Fact]
    public async Task GetMe_ServiceFailure_Returns400WithProblemDetails()
    {
        _factory.UserService.GetCurrentUserAsync(Arg.Any<CancellationToken>())
            .Returns(Result<UserOutput>.Failure(ErrorMessages.User.NotFound));

        var response = await _client.SendAsync(Get("/api/users/me", TestAuth.User()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemDetailsAsync(response, 400, ErrorMessages.User.NotFound);
    }

    #endregion

    #region UpdateMe

    [Fact]
    public async Task UpdateMe_Authenticated_Returns200()
    {
        _factory.UserService.UpdateProfileAsync(
                Arg.Any<UpdateProfileInput>(), Arg.Any<CancellationToken>())
            .Returns(Result<UserOutput>.Success(new UserOutput(
                Guid.NewGuid(), "test@example.com", "Jane", "Doe",
                null, null, null, ["User"], [])));

        var response = await _client.SendAsync(
            Patch("/api/users/me", JsonContent.Create(new { FirstName = "Jane" }), TestAuth.User()));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UserMeResponse>();
        Assert.NotNull(body);
        Assert.Equal("Jane", body.FirstName);
    }

    [Fact]
    public async Task UpdateMe_Unauthenticated_Returns401()
    {
        var response = await _client.SendAsync(
            Patch("/api/users/me", JsonContent.Create(new { FirstName = "Jane" })));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region DeleteMe

    [Fact]
    public async Task DeleteMe_Authenticated_Returns204()
    {
        _factory.UserService.DeleteAccountAsync(
                Arg.Any<DeleteAccountInput>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var response = await _client.SendAsync(
            Delete("/api/users/me", JsonContent.Create(new { Password = "MyPassword1!" }), TestAuth.User()));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteMe_Unauthenticated_Returns401()
    {
        var response = await _client.SendAsync(
            Delete("/api/users/me", JsonContent.Create(new { Password = "MyPassword1!" })));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteMe_ServiceFailure_Returns400WithProblemDetails()
    {
        _factory.UserService.DeleteAccountAsync(
                Arg.Any<DeleteAccountInput>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(ErrorMessages.User.DeleteInvalidPassword));

        var response = await _client.SendAsync(
            Delete("/api/users/me", JsonContent.Create(new { Password = "WrongPass1!" }), TestAuth.User()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemDetailsAsync(response, 400, ErrorMessages.User.DeleteInvalidPassword);
    }

    #endregion
}
