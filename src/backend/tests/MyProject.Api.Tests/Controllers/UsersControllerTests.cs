using System.Net;
using System.Net.Http.Json;
using MyProject.Api.Tests.Fixtures;
using MyProject.Application.Features.Authentication.Dtos;
using MyProject.Application.Identity.Dtos;
using MyProject.Shared;

namespace MyProject.Api.Tests.Controllers;

public class UsersControllerTests : IClassFixture<CustomWebApplicationFactory>, IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _authenticatedClient;
    private readonly HttpClient _anonymousClient;

    public UsersControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _authenticatedClient = factory.CreateClient();
        _authenticatedClient.DefaultRequestHeaders.Add("Authorization", "Test token");
        _anonymousClient = factory.CreateClient();
    }

    public void Dispose()
    {
        _authenticatedClient.Dispose();
        _anonymousClient.Dispose();
    }

    [Fact]
    public async Task GetMe_Authenticated_Returns200()
    {
        _factory.UserService.GetCurrentUserAsync(Arg.Any<CancellationToken>())
            .Returns(Result<UserOutput>.Success(new UserOutput(
                Guid.NewGuid(), "test@example.com", "John", "Doe",
                null, null, null, ["User"], [])));

        var response = await _authenticatedClient.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_Unauthenticated_Returns401()
    {
        var response = await _anonymousClient.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateMe_Authenticated_Returns200()
    {
        _factory.UserService.UpdateProfileAsync(
                Arg.Any<UpdateProfileInput>(), Arg.Any<CancellationToken>())
            .Returns(Result<UserOutput>.Success(new UserOutput(
                Guid.NewGuid(), "test@example.com", "Jane", "Doe",
                null, null, null, ["User"], [])));

        var response = await _authenticatedClient.PatchAsJsonAsync("/api/users/me", new
        {
            FirstName = "Jane"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateMe_Unauthenticated_Returns401()
    {
        var response = await _anonymousClient.PatchAsJsonAsync("/api/users/me", new
        {
            FirstName = "Jane"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteMe_Authenticated_Returns204()
    {
        _factory.UserService.DeleteAccountAsync(
                Arg.Any<DeleteAccountInput>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var request = new HttpRequestMessage(HttpMethod.Delete, "/api/users/me")
        {
            Content = JsonContent.Create(new { Password = "MyPassword1!" })
        };
        var response = await _authenticatedClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteMe_Unauthenticated_Returns401()
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, "/api/users/me")
        {
            Content = JsonContent.Create(new { Password = "MyPassword1!" })
        };
        var response = await _anonymousClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
