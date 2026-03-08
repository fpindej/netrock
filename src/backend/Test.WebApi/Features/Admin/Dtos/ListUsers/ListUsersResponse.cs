using Test.WebApi.Shared;

namespace Test.WebApi.Features.Admin.Dtos.ListUsers;

/// <summary>
/// Paginated response containing a list of admin user records.
/// </summary>
public class ListUsersResponse : PaginatedResponse
{
    /// <summary>
    /// The users for the current page.
    /// </summary>
    public IReadOnlyList<AdminUserResponse> Items { get; init; } = [];
}
