using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyProject.Application.Identity.Constants;

namespace MyProject.Api.Tests.Fixtures;

/// <summary>
/// Test authentication handler that builds claims from the Authorization header value.
/// <para>
/// Header format: <c>Test [roles=Role1,Role2] [permissions=perm1,perm2] [userId=guid] [email=addr]</c>
/// </para>
/// <para>
/// Examples:
/// <list type="bullet">
///   <item><c>Authorization: Test</c> — default User role, no permissions</item>
///   <item><c>Authorization: Test roles=Admin permissions=users.view,users.manage</c></item>
///   <item><c>Authorization: Test roles=SuperAdmin</c> — bypasses all permission checks</item>
/// </list>
/// </para>
/// </summary>
public class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";
    public const string DefaultUserId = "00000000-0000-0000-0000-000000000001";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Context.Request.Headers.TryGetValue("Authorization", out var header) ||
            string.IsNullOrEmpty(header.ToString()))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var headerValue = header.ToString();
        if (!headerValue.StartsWith("Test", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var config = ParseHeader(headerValue);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, config.UserId),
            new(ClaimTypes.Name, config.Email),
            new(ClaimTypes.Email, config.Email)
        };

        foreach (var role in config.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        foreach (var permission in config.Permissions)
        {
            claims.Add(new Claim(AppPermissions.ClaimType, permission));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private static ParsedConfig ParseHeader(string headerValue)
    {
        var parts = headerValue.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        string userId = DefaultUserId;
        string email = "test@example.com";
        string[] roles = [AppRoles.User];
        string[] permissions = [];

        foreach (var part in parts.Skip(1))
        {
            var eqIndex = part.IndexOf('=');
            if (eqIndex < 0) continue;

            var key = part[..eqIndex];
            var value = part[(eqIndex + 1)..];

            switch (key)
            {
                case "userId":
                    userId = value;
                    break;
                case "email":
                    email = value;
                    break;
                case "roles":
                    roles = value.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    break;
                case "permissions":
                    permissions = value.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    break;
            }
        }

        return new ParsedConfig(userId, email, roles, permissions);
    }

    private record ParsedConfig(string UserId, string Email, string[] Roles, string[] Permissions);
}

/// <summary>
/// Helper to build Authorization header values for <see cref="TestAuthHandler"/>.
/// </summary>
internal static class TestAuth
{
    public static string User() => "Test";

    public static string WithPermissions(params string[] permissions) =>
        $"Test permissions={string.Join(',', permissions)}";

    public static string Admin(params string[] permissions) =>
        permissions.Length > 0
            ? $"Test roles=Admin permissions={string.Join(',', permissions)}"
            : "Test roles=Admin";

    public static string SuperAdmin() => "Test roles=SuperAdmin";
}
