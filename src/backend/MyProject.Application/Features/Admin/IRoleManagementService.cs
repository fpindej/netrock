using MyProject.Application.Features.Admin.Dtos;
using MyProject.Shared;

namespace MyProject.Application.Features.Admin;

/// <summary>
/// Provides CRUD operations for roles and permission management.
/// <para>
/// System roles (SuperAdmin, Admin, User) cannot be deleted or renamed.
/// SuperAdmin permissions are implicit and cannot be modified via this service.
/// </para>
/// </summary>
public interface IRoleManagementService
{
    /// <summary>
    /// Gets detailed information about a single role, including its permissions and user count.
    /// </summary>
    /// <param name="roleId">The role ID.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The role details, or a failure if not found.</returns>
    Task<Result<RoleDetailOutput>> GetRoleDetailAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new custom role.
    /// </summary>
    /// <param name="input">The role name and optional description.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The new role's ID, or a failure if the name is taken.</returns>
    Task<Result<Guid>> CreateRoleAsync(CreateRoleInput input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing role's name and/or description.
    /// System roles cannot be renamed.
    /// </summary>
    /// <param name="roleId">The role ID.</param>
    /// <param name="input">The fields to update.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Success or failure with an error message.</returns>
    Task<Result> UpdateRoleAsync(Guid roleId, UpdateRoleInput input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a custom role. System roles and roles with assigned users cannot be deleted.
    /// </summary>
    /// <param name="roleId">The role ID.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Success or failure with an error message.</returns>
    Task<Result> DeleteRoleAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces all permission claims on a role. Rotates security stamps for all users in the role.
    /// Cannot modify SuperAdmin permissions.
    /// </summary>
    /// <param name="roleId">The role ID.</param>
    /// <param name="input">The new set of permissions.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Success or failure with an error message.</returns>
    Task<Result> SetRolePermissionsAsync(Guid roleId, SetRolePermissionsInput input,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all available permissions grouped by category.
    /// </summary>
    /// <returns>The permission groups.</returns>
    IReadOnlyList<PermissionGroupOutput> GetAllPermissions();
}
