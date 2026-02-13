using JetBrains.Annotations;

namespace MyProject.WebApi.Features.Admin.Dtos;

/// <summary>
/// A group of permissions belonging to the same category.
/// </summary>
public class PermissionGroupResponse
{
    /// <summary>
    /// The category name (e.g. "Users", "Roles").
    /// </summary>
    public string Category { [UsedImplicitly] get; [UsedImplicitly] init; } = string.Empty;

    /// <summary>
    /// The permission values in this category.
    /// </summary>
    public IReadOnlyList<string> Permissions { [UsedImplicitly] get; [UsedImplicitly] init; } = [];
}
