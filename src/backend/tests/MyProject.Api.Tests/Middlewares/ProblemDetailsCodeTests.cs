using System.Net;
using System.Net.Http.Json;
using MyProject.Api.Tests.Fixtures;
using MyProject.Application.Features.Authentication.Dtos;
using MyProject.Shared;
using MyProject.WebApi.Shared;
using NSubstitute.ExceptionExtensions;

namespace MyProject.Api.Tests.Middlewares;

/// <summary>
/// Verifies that every <c>ProblemDetails</c> response carries a machine-readable <c>code</c> extension,
/// regardless of which part of the pipeline produced it (controller, authorization handler,
/// exception middleware, model validation, status code pages).
/// </summary>
public class ProblemDetailsCodeTests : IClassFixture<CustomWebApplicationFactory>, IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ProblemDetailsCodeTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetMocks();
        _client = factory.CreateClient();
    }

    public void Dispose() => _client.Dispose();

    [Fact]
    public async Task ControllerFailure_IncludesErrorCode()
    {
        _factory.UserService.GetCurrentUserAsync(Arg.Any<CancellationToken>())
            .Returns(Result<UserOutput>.Failure(ErrorMessages.User.NotFound, ErrorType.NotFound));

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        request.Headers.Add("Authorization", TestAuth.User());

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await ProblemDetailsAssert.MatchesAsync(response, 404, ErrorMessages.User.NotFound);
    }

    [Fact]
    public async Task Unauthenticated_IncludesErrorCode()
    {
        var response = await _client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await ProblemDetailsAssert.MatchesAsync(response, 401, ErrorMessages.Auth.NotAuthenticated);
    }

    [Fact]
    public async Task Forbidden_IncludesErrorCode()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/users?pageNumber=1&pageSize=10");
        request.Headers.Add("Authorization", TestAuth.User());

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await ProblemDetailsAssert.MatchesAsync(response, 403, ErrorMessages.Auth.InsufficientPermissions);
    }

    [Fact]
    public async Task ModelValidationFailure_IncludesValidationFailedCode()
    {
        var response = await _client.PostAsync(
            "/api/auth/login",
            JsonContent.Create(new { Username = "", Password = "" }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await ProblemDetailsAssert.HasCodeAsync(response, 400, ProblemFactory.ValidationFailedCode);
    }

    [Fact]
    public async Task UnmatchedRoute_IncludesFallbackCode()
    {
        var response = await _client.GetAsync("/api/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await ProblemDetailsAssert.HasCodeAsync(response, 404, "not_found");
    }

    [Fact]
    public async Task UnhandledException_IncludesErrorCode()
    {
        _factory.UserService.GetCurrentUserAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("boom"));

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        request.Headers.Add("Authorization", TestAuth.User());

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        await ProblemDetailsAssert.MatchesAsync(response, 500, ErrorMessages.Server.InternalError);
    }

    [Fact]
    public async Task KeyNotFoundException_IncludesEntityNotFoundCode()
    {
        _factory.UserService.GetCurrentUserAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new KeyNotFoundException("missing"));

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        request.Headers.Add("Authorization", TestAuth.User());

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await ProblemDetailsAssert.MatchesAsync(response, 404, ErrorMessages.Entity.NotFound);
    }
}
