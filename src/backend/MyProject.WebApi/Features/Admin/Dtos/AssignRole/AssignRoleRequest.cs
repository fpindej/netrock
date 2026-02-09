using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace MyProject.WebApi.Features.Admin.Dtos.AssignRole;

/// <summary>
/// Request to assign a role to a user.
/// </summary>
public class AssignRoleRequest
{
    /// <summary>
    /// The name of the role to assign.
    /// </summary>
    [Required]
    public string Role { get; [UsedImplicitly] init; } = string.Empty;
}
