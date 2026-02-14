using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Mvc;
using MyProject.Domain;

namespace MyProject.WebApi.Authorization;

/// <summary>
/// Intercepts authorization failures (both challenged and forbidden) and writes a
/// standardized <see cref="ProblemDetails"/> JSON body so that 401/403 responses match the
/// OpenAPI specification. Without this handler, ASP.NET Core's default returns empty bodies.
/// </summary>
internal sealed class ProblemDetailsAuthorizationHandler : IAuthorizationMiddlewareResultHandler
{
    /// <inheritdoc />
    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext httpContext,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Challenged)
        {
            httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            httpContext.Response.Headers.WWWAuthenticate = "Bearer";
            httpContext.Response.ContentType = "application/problem+json";

            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Detail = ErrorMessages.Auth.NotAuthenticated,
                Instance = httpContext.Request.Path
            };

            await httpContext.Response.WriteAsJsonAsync(problemDetails);
            return;
        }

        if (authorizeResult.Forbidden)
        {
            httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
            httpContext.Response.ContentType = "application/problem+json";

            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Forbidden",
                Detail = ErrorMessages.Auth.InsufficientPermissions,
                Instance = httpContext.Request.Path
            };

            await httpContext.Response.WriteAsJsonAsync(problemDetails);
            return;
        }

        await next(httpContext);
    }
}
