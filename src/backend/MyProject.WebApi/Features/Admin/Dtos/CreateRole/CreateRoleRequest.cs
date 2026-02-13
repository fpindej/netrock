using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace MyProject.WebApi.Features.Admin.Dtos.CreateRole;

/// <summary>
/// Request to create a new custom role.
/// </summary>
public class CreateRoleRequest
{
    /// <summary>
    /// The name of the new role.
    /// </summary>
    [Required]
    public string Name { get; [UsedImplicitly] init; } = string.Empty;

    /// <summary>
    /// An optional description of the role's purpose.
    /// </summary>
    public string? Description { get; [UsedImplicitly] init; }
}
