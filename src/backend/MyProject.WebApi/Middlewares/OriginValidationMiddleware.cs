using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MyProject.Shared;
using CorsOptions = MyProject.WebApi.Options.CorsOptions;

namespace MyProject.WebApi.Middlewares;

/// <summary>
/// Validates the <c>Origin</c> header on state-changing requests (POST, PUT, PATCH, DELETE)
/// to prevent cross-site request forgery when cookies use <c>SameSite=None</c>.
/// </summary>
/// <remarks>
/// <para>
/// This is a defense-in-depth measure. The SvelteKit frontend proxy performs the same check,
/// but this middleware protects the API if the proxy is bypassed (direct API calls with cookies).
/// </para>
/// <para>
/// Requests without an <c>Origin</c> header are allowed through because a missing header on an
/// unsafe method means either a same-origin request from an older browser or a non-browser client —
/// neither of which is vulnerable to CSRF.
/// </para>
/// <para>Pattern documented in src/backend/AGENTS.md — update both when changing.</para>
/// </remarks>
public class OriginValidationMiddleware(
    RequestDelegate next,
    IOptions<CorsOptions> corsOptions,
    ILogger<OriginValidationMiddleware> logger,
    IProblemDetailsService problemDetailsService)
{
    private static readonly HashSet<string> UnsafeMethods =
        new(["POST", "PUT", "PATCH", "DELETE"], StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Validates the Origin header and either passes the request through or returns 403.
    /// </summary>
    public async Task Invoke(HttpContext context)
    {
        if (!UnsafeMethods.Contains(context.Request.Method))
        {
            await next(context);
            return;
        }

        var origin = context.Request.Headers.Origin.ToString();

        if (string.IsNullOrEmpty(origin))
        {
            await next(context);
            return;
        }

        var options = corsOptions.Value;

        if (options.AllowAllOrigins)
        {
            await next(context);
            return;
        }

        if (options.AllowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        logger.LogWarning(
            "Blocked cross-origin {Method} request from {Origin} to {Path}",
            context.Request.Method,
            origin,
            context.Request.Path);

        context.Response.StatusCode = StatusCodes.Status403Forbidden;

        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Detail = ErrorMessages.Security.CrossOriginRequestBlocked
            }
        });
    }
}
