using System.ComponentModel;
using System.Text.Json;
using MyProject.Application.Features.Admin;
using ModelContextProtocol.Server;

namespace MyProject.WebApi.Mcp;

/// <summary>
/// MCP tools for querying user data through the admin service.
/// </summary>
[McpServerToolType]
internal static class AdminTools
{
    /// <summary>
    /// Lists users with optional search and pagination, using the existing admin service.
    /// </summary>
    [McpServerTool(Name = "list-users"), Description("List users with optional search and pagination.")]
    public static async Task<string> ListUsers(
        IAdminService adminService,
        [Description("Optional search term to filter by name or email.")] string? search = null,
        [Description("Page number (1-based). Defaults to 1.")] int pageNumber = 1,
        [Description("Page size. Defaults to 20.")] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await adminService.GetUsersAsync(pageNumber, pageSize, search, cancellationToken);

        return JsonSerializer.Serialize(result, JsonSerializerOptions.Web);
    }
}
