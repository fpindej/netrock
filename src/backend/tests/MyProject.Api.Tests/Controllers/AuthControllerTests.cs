using System.Net;
using System.Net.Http.Json;
using MyProject.Api.Tests.Fixtures;
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
        _client = factory.CreateClient();
    }

    public void Dispose() => _client.Dispose();

    [Fact]
    public async Task Login_ValidCredentials_Returns200()
    {
        _factory.AuthenticationService.Login(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Result<AuthenticationOutput>.Success(new AuthenticationOutput("access", "refresh")));

        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            Username = "test@example.com",
            Password = "Password1!"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_InvalidCredentials_Returns401()
    {
        _factory.AuthenticationService.Login(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Result<AuthenticationOutput>.Failure(
                ErrorMessages.Auth.LoginInvalidCredentials, ErrorType.Unauthorized));

        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            Username = "test@example.com",
            Password = "wrong"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_MissingEmail_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            Username = "",
            Password = "Password1!"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_ValidInput_Returns201()
    {
        _factory.AuthenticationService.Register(Arg.Any<RegisterInput>(), Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = "new@example.com",
            Password = "Password1!"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidEmail_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = "not-an-email",
            Password = "Password1!"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WeakPassword_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = "test@example.com",
            Password = "weak"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Logout_Authenticated_Returns204()
    {
        _client.DefaultRequestHeaders.Add("Authorization", "Test token");

        var response = await _client.PostAsync("/api/auth/logout", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Logout_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/auth/logout", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        client.Dispose();
    }

    [Fact]
    public async Task ChangePassword_Authenticated_Returns204()
    {
        _client.DefaultRequestHeaders.Add("Authorization", "Test token");
        _factory.AuthenticationService.ChangePasswordAsync(
                Arg.Any<ChangePasswordInput>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var response = await _client.PostAsJsonAsync("/api/auth/change-password", new
        {
            CurrentPassword = "OldPass1!",
            NewPassword = "NewPass1!"
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/change-password", new
        {
            CurrentPassword = "OldPass1!",
            NewPassword = "NewPass1!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        client.Dispose();
    }
}
