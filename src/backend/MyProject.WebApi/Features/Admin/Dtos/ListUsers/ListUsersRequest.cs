using System.ComponentModel;
using JetBrains.Annotations;
using MyProject.WebApi.Shared;

namespace MyProject.WebApi.Features.Admin.Dtos.ListUsers;

/// <summary>
/// Request parameters for listing users with optional search filtering.
/// </summary>
public class ListUsersRequest : PaginatedRequest
{
    /// <summary>
    /// Optional search term to filter users by name or email.
    /// </summary>
    [Description("Optional search term to filter users by name or email.")]
    public string? Search { get; [UsedImplicitly] set; }
}
