using JetBrains.Annotations;

namespace MyProject.WebApi.Features.Admin.Dtos.UpdateRole;

/// <summary>
/// Request to update an existing role's name and/or description.
/// </summary>
public class UpdateRoleRequest
{
    /// <summary>
    /// The new role name, or <c>null</c> to keep the current name.
    /// </summary>
    public string? Name { get; [UsedImplicitly] init; }

    /// <summary>
    /// The new description, or <c>null</c> to keep the current description.
    /// </summary>
    public string? Description { get; [UsedImplicitly] init; }
}
