using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyProject.Shared;
using MyProject.WebApi.Shared;

namespace MyProject.Api.Tests.Shared;

public class ProblemFactoryTests
{
    private static readonly Error TestError = new("test_error", "Something went wrong.");

    [Theory]
    [InlineData(ErrorType.Validation, 400)]
    [InlineData(ErrorType.Unauthorized, 401)]
    [InlineData(ErrorType.Forbidden, 403)]
    [InlineData(ErrorType.NotFound, 404)]
    [InlineData(null, 400)]
    public void Create_MapsErrorTypeToStatusAndCarriesDetailAndCode(ErrorType? errorType, int expectedStatus)
    {
        var result = ProblemFactory.Create(TestError, errorType);

        var problem = Assert.IsType<ProblemDetails>(result.Value);
        Assert.Equal(expectedStatus, result.StatusCode);
        Assert.Equal(expectedStatus, problem.Status);
        Assert.Equal(TestError.Message, problem.Detail);
        Assert.Equal(TestError.Code, problem.Extensions[ProblemFactory.CodeExtensionKey]);
    }

    [Fact]
    public void Create_WithNullError_OmitsDetailAndCode()
    {
        var result = ProblemFactory.Create(null, ErrorType.NotFound);

        var problem = Assert.IsType<ProblemDetails>(result.Value);
        Assert.Equal(404, problem.Status);
        Assert.Null(problem.Detail);
        Assert.False(problem.Extensions.ContainsKey(ProblemFactory.CodeExtensionKey));
    }

    [Fact]
    public void CreateProblemDetails_CarriesStatusDetailAndCode()
    {
        var problem = ProblemFactory.CreateProblemDetails(TestError, StatusCodes.Status429TooManyRequests);

        Assert.Equal(429, problem.Status);
        Assert.Equal(TestError.Message, problem.Detail);
        Assert.Equal(TestError.Code, problem.Extensions[ProblemFactory.CodeExtensionKey]);
    }

    [Fact]
    public void EnsureCode_PreservesExistingCode()
    {
        var problem = ProblemFactory.CreateProblemDetails(TestError, StatusCodes.Status400BadRequest);

        ProblemFactory.EnsureCode(problem);

        Assert.Equal(TestError.Code, problem.Extensions[ProblemFactory.CodeExtensionKey]);
    }

    [Fact]
    public void EnsureCode_ValidationProblem_UsesValidationFailedCode()
    {
        var problem = new HttpValidationProblemDetails { Status = StatusCodes.Status400BadRequest };

        ProblemFactory.EnsureCode(problem);

        Assert.Equal(ProblemFactory.ValidationFailedCode, problem.Extensions[ProblemFactory.CodeExtensionKey]);
    }

    [Theory]
    [InlineData(400, "bad_request")]
    [InlineData(404, "not_found")]
    [InlineData(405, "method_not_allowed")]
    [InlineData(415, "unsupported_media_type")]
    [InlineData(500, "internal_server_error")]
    public void EnsureCode_WithoutCode_FallsBackToSnakeCaseReasonPhrase(int status, string expectedCode)
    {
        var problem = new ProblemDetails { Status = status };

        ProblemFactory.EnsureCode(problem);

        Assert.Equal(expectedCode, problem.Extensions[ProblemFactory.CodeExtensionKey]);
    }

    [Fact]
    public void EnsureCode_WithoutStatus_TreatsAsInternalServerError()
    {
        var problem = new ProblemDetails();

        ProblemFactory.EnsureCode(problem);

        Assert.Equal("internal_server_error", problem.Extensions[ProblemFactory.CodeExtensionKey]);
    }
}
