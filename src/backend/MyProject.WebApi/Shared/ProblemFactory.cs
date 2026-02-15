using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using MyProject.Shared;

namespace MyProject.WebApi.Shared;

/// <summary>
/// Creates <see cref="ProblemDetails"/>-based action results from atomic error values.
/// Produces a consistent <see cref="ProblemDetails"/> body with Title set from the
/// status code's reason phrase.
/// </summary>
internal static class ProblemFactory
{
    /// <summary>
    /// Returns a <see cref="ProblemDetails"/> response with the specified detail and error type.
    /// </summary>
    /// <param name="detail">The error detail message.</param>
    /// <param name="errorType">The error category. Defaults to 400 Bad Request when <c>null</c>.</param>
    public static ObjectResult Create(string? detail, ErrorType? errorType = null)
    {
        var code = ToStatusCode(errorType);

        var problemDetails = new ProblemDetails
        {
            Status = code,
            Detail = detail,
            Title = ReasonPhrases.GetReasonPhrase(code),
            Type = $"https://tools.ietf.org/html/rfc9110#section-15.5.{code - 399}"
        };

        return new ObjectResult(problemDetails) { StatusCode = code };
    }

    private static int ToStatusCode(ErrorType? errorType) => errorType switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        _ => StatusCodes.Status400BadRequest
    };
}
