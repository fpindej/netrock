using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace MyProject.WebApi.Features.Admin.Dtos.SetPermissions;

/// <summary>
/// Request to replace all permissions on a role.
/// </summary>
public class SetPermissionsRequest
{
    /// <summary>
    /// The full set of permission values to assign to the role.
    /// </summary>
    [Required]
    public IReadOnlyList<string> Permissions { get; [UsedImplicitly] init; } = [];
}
