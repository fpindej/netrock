using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using MyProject.Domain;
using MyProject.WebApi.Shared;

namespace MyProject.WebApi.Authorization;

/// <summary>
/// Intercepts authorization failures (both challenged and forbidden) and writes a
/// standardized <see cref="ErrorResponse"/> JSON body so that 401/403 responses match the
/// OpenAPI specification. Without this handler, ASP.NET Core's default returns empty bodies.
/// </summary>
internal sealed class ErrorResponseAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

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
            httpContext.Response.ContentType = "application/json";
            await httpContext.Response.WriteAsync(
                JsonSerializer.Serialize(
                    new ErrorResponse { Message = ErrorMessages.Auth.NotAuthenticated },
                    JsonOptions));
            return;
        }

        if (authorizeResult.Forbidden)
        {
            httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
            httpContext.Response.ContentType = "application/json";
            await httpContext.Response.WriteAsync(
                JsonSerializer.Serialize(
                    new ErrorResponse { Message = ErrorMessages.Auth.InsufficientPermissions },
                    JsonOptions));
            return;
        }

        await next(httpContext);
    }
}
