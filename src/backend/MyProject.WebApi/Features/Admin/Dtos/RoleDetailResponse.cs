using JetBrains.Annotations;

namespace MyProject.WebApi.Features.Admin.Dtos;

/// <summary>
/// Detailed role response including permissions and user count.
/// </summary>
public class RoleDetailResponse
{
    /// <summary>
    /// The unique identifier of the role.
    /// </summary>
    public Guid Id { [UsedImplicitly] get; [UsedImplicitly] init; }

    /// <summary>
    /// The name of the role.
    /// </summary>
    public string Name { [UsedImplicitly] get; [UsedImplicitly] init; } = string.Empty;

    /// <summary>
    /// An optional description of the role's purpose.
    /// </summary>
    public string? Description { [UsedImplicitly] get; [UsedImplicitly] init; }

    /// <summary>
    /// Whether this is a system-defined role that cannot be deleted or renamed.
    /// </summary>
    public bool IsSystem { [UsedImplicitly] get; [UsedImplicitly] init; }

    /// <summary>
    /// The permission claim values assigned to this role.
    /// </summary>
    public IReadOnlyList<string> Permissions { [UsedImplicitly] get; [UsedImplicitly] init; } = [];

    /// <summary>
    /// The number of users assigned to this role.
    /// </summary>
    public int UserCount { [UsedImplicitly] get; [UsedImplicitly] init; }
}
