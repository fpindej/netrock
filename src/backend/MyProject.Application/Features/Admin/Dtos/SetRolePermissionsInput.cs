namespace MyProject.Application.Features.Admin.Dtos;

/// <summary>
/// Input for replacing all permissions on a role.
/// </summary>
/// <param name="Permissions">The full set of permission values to assign.</param>
public record SetRolePermissionsInput(IReadOnlyList<string> Permissions);
