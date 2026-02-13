namespace MyProject.Application.Features.Admin.Dtos;

/// <summary>
/// Input for updating an existing role's name and/or description.
/// </summary>
/// <param name="Name">The new role name, or <c>null</c> to keep the current name.</param>
/// <param name="Description">The new description, or <c>null</c> to keep the current description.</param>
public record UpdateRoleInput(string? Name, string? Description);
