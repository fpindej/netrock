using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using MyProject.Shared;

namespace MyProject.WebApi.Shared;

/// <summary>
/// Creates <see cref="ProblemDetails"/> bodies and action results from <see cref="Error"/> values.
/// Every body carries the human-readable message in <c>detail</c> and the stable, machine-readable
/// error code in the <c>code</c> extension so clients never have to match on English text.
/// </summary>
internal static class ProblemFactory
{
    /// <summary>
    /// Name of the <see cref="ProblemDetails.Extensions"/> entry that carries the machine-readable error code.
    /// </summary>
    public const string CodeExtensionKey = "code";

    /// <summary>
    /// Code applied to framework-generated validation failures (<see cref="HttpValidationProblemDetails"/>),
    /// whose field-level details live in <c>errors</c>.
    /// </summary>
    public const string ValidationFailedCode = "validation_failed";

    /// <summary>
    /// Returns a <see cref="ProblemDetails"/> action result for the specified error and error type.
    /// </summary>
    /// <param name="error">The error to report. Null is tolerated for defensive call sites and yields no detail or code.</param>
    /// <param name="errorType">The error category. Defaults to 400 Bad Request when <c>null</c>.</param>
    public static ObjectResult Create(Error? error, ErrorType? errorType = null)
    {
        var statusCode = ToStatusCode(errorType);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = ReasonPhrases.GetReasonPhrase(statusCode),
            Type = $"https://tools.ietf.org/html/rfc9110#section-15.5.{statusCode - 399}"
        };

        if (error is not null)
        {
            problemDetails.Detail = error.Message;
            problemDetails.Extensions[CodeExtensionKey] = error.Code;
        }

        return new ObjectResult(problemDetails) { StatusCode = statusCode };
    }

    /// <summary>
    /// Creates a bare <see cref="ProblemDetails"/> body (status, detail, code) for middleware and handlers
    /// that write through <see cref="IProblemDetailsService"/>, which fills in title and type defaults.
    /// </summary>
    /// <param name="error">The error to report.</param>
    /// <param name="statusCode">The HTTP status code of the response.</param>
    public static ProblemDetails CreateProblemDetails(Error error, int statusCode)
    {
        return new ProblemDetails
        {
            Status = statusCode,
            Detail = error.Message,
            Extensions = { [CodeExtensionKey] = error.Code }
        };
    }

    /// <summary>
    /// Ensures a <c>code</c> extension is present on framework-generated problem details
    /// (model validation, status code pages, unmatched routes). Bodies that already carry a code are left untouched.
    /// Validation failures get <see cref="ValidationFailedCode"/>; everything else falls back to the
    /// snake_case reason phrase of the status (for example <c>not_found</c>, <c>method_not_allowed</c>).
    /// </summary>
    /// <param name="problemDetails">The problem details being written.</param>
    public static void EnsureCode(ProblemDetails problemDetails)
    {
        if (problemDetails.Extensions.ContainsKey(CodeExtensionKey))
        {
            return;
        }

        problemDetails.Extensions[CodeExtensionKey] = problemDetails is HttpValidationProblemDetails
            ? ValidationFailedCode
            : ToFallbackCode(problemDetails.Status ?? StatusCodes.Status500InternalServerError);
    }

    private static string ToFallbackCode(int statusCode)
    {
        var reasonPhrase = ReasonPhrases.GetReasonPhrase(statusCode);

        return string.IsNullOrEmpty(reasonPhrase)
            ? $"http_{statusCode}"
            : reasonPhrase.Replace(' ', '_').Replace('-', '_').ToLowerInvariant();
    }

    private static int ToStatusCode(ErrorType? errorType) => errorType switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status400BadRequest
    };
}
