using JetBrains.Annotations;

namespace MyProject.WebApi.Features.Admin.Dtos;

/// <summary>
/// Represents a role with its associated user count.
/// </summary>
public class AdminRoleResponse
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
    /// The number of users assigned to this role.
    /// </summary>
    public int UserCount { [UsedImplicitly] get; [UsedImplicitly] init; }
}
