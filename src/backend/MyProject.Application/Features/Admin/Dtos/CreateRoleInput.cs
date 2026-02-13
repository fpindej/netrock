namespace MyProject.Application.Features.Admin.Dtos;

/// <summary>
/// Input for creating a new custom role.
/// </summary>
/// <param name="Name">The role name.</param>
/// <param name="Description">An optional description of the role's purpose.</param>
public record CreateRoleInput(string Name, string? Description);
