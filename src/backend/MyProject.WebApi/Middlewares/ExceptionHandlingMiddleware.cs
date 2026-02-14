using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using MyProject.Domain;
using MyProject.Infrastructure.Persistence.Exceptions;

namespace MyProject.WebApi.Middlewares;

/// <summary>
/// Catches unhandled exceptions and maps them to standardized <see cref="ProblemDetails"/> JSON responses.
/// </summary>
/// <remarks>Pattern documented in src/backend/AGENTS.md — update both when changing.</remarks>
public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IHostEnvironment env)
{
    /// <summary>
    /// Invokes the next middleware and catches exceptions, mapping them to HTTP status codes:
    /// <see cref="KeyNotFoundException"/> to 404,
    /// <see cref="PaginationException"/> to 400,
    /// and all others to 500.
    /// </summary>
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next.Invoke(context);
        }
        catch (KeyNotFoundException keyNotFoundEx)
        {
            logger.LogWarning(keyNotFoundEx, "A KeyNotFoundException occurred.");
            await HandleExceptionAsync(context, keyNotFoundEx, HttpStatusCode.NotFound);
        }
        catch (PaginationException paginationEx)
        {
            logger.LogWarning(paginationEx, "A PaginationException occurred.");
            await HandleExceptionAsync(context, paginationEx, HttpStatusCode.BadRequest);
        }
        catch (Exception e)
        {
            logger.LogError(e, "An unhandled exception occurred.");
            await HandleExceptionAsync(context, e, HttpStatusCode.InternalServerError,
                customMessage: ErrorMessages.Server.InternalError);
        }
    }

    private async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception,
        HttpStatusCode statusCode,
        string? customMessage = null)
    {
        var status = (int)statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = status,
            Title = ReasonPhrases.GetReasonPhrase(status),
            Detail = customMessage ?? exception.Message,
            Instance = context.Request.Path
        };

        if (env.IsDevelopment() && exception.StackTrace is not null)
        {
            problemDetails.Extensions["stackTrace"] = exception.StackTrace;
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = status;

        await context.Response.WriteAsJsonAsync(problemDetails);
    }
}
