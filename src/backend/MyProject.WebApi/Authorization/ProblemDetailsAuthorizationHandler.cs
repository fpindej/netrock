using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Mvc;
using MyProject.Shared;

namespace MyProject.WebApi.Authorization;

/// <summary>
/// Intercepts authorization failures (both challenged and forbidden) and writes a
/// standardized <see cref="ProblemDetails"/> JSON body so that 401/403 responses match the
/// OpenAPI specification. Without this handler, ASP.NET Core's default returns empty bodies.
/// </summary>
internal sealed class ProblemDetailsAuthorizationHandler(
    IProblemDetailsService problemDetailsService) : IAuthorizationMiddlewareResultHandler
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

            await problemDetailsService.WriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Detail = ErrorMessages.Auth.NotAuthenticated
                }
            });
            return;
        }

        if (authorizeResult.Forbidden)
        {
            httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;

            await problemDetailsService.WriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Detail = ErrorMessages.Auth.InsufficientPermissions
                }
            });
            return;
        }

        await next(httpContext);
    }
}
