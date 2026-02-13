namespace MyProject.Application.Features.Admin.Dtos;

/// <summary>
/// Detailed role output including permissions and user count.
/// </summary>
/// <param name="Id">The role's unique identifier.</param>
/// <param name="Name">The role name.</param>
/// <param name="Description">An optional description of the role's purpose.</param>
/// <param name="IsSystem">Whether this is a system-defined role that cannot be deleted or renamed.</param>
/// <param name="Permissions">The permission claim values assigned to this role.</param>
/// <param name="UserCount">The number of users assigned to this role.</param>
public record RoleDetailOutput(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystem,
    IReadOnlyList<string> Permissions,
    int UserCount
);
