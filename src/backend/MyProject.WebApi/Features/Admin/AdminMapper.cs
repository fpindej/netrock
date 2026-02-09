using MyProject.Application.Features.Admin.Dtos;
using MyProject.WebApi.Features.Admin.Dtos;
using MyProject.WebApi.Features.Admin.Dtos.AssignRole;
using MyProject.WebApi.Features.Admin.Dtos.ListUsers;

namespace MyProject.WebApi.Features.Admin;

/// <summary>
/// Maps between admin Application layer DTOs and WebApi request/response DTOs.
/// </summary>
internal static class AdminMapper
{
    /// <summary>
    /// Maps an <see cref="AdminUserOutput"/> to an <see cref="AdminUserResponse"/>.
    /// </summary>
    public static AdminUserResponse ToResponse(this AdminUserOutput output) => new()
    {
        Id = output.Id,
        Username = output.UserName,
        Email = output.Email,
        FirstName = output.FirstName,
        LastName = output.LastName,
        PhoneNumber = output.PhoneNumber,
        Bio = output.Bio,
        AvatarUrl = output.AvatarUrl,
        Roles = output.Roles,
        EmailConfirmed = output.EmailConfirmed,
        LockoutEnabled = output.LockoutEnabled,
        LockoutEnd = output.LockoutEnd,
        AccessFailedCount = output.AccessFailedCount,
        IsLockedOut = output.IsLockedOut
    };

    /// <summary>
    /// Maps an <see cref="AdminUserListOutput"/> to a <see cref="ListUsersResponse"/>.
    /// </summary>
    public static ListUsersResponse ToResponse(this AdminUserListOutput output) => new()
    {
        Items = output.Users.Select(u => u.ToResponse()).ToList(),
        TotalCount = output.TotalCount,
        PageNumber = output.PageNumber,
        PageSize = output.PageSize
    };

    /// <summary>
    /// Maps an <see cref="AdminRoleOutput"/> to an <see cref="AdminRoleResponse"/>.
    /// </summary>
    public static AdminRoleResponse ToResponse(this AdminRoleOutput output) => new()
    {
        Id = output.Id,
        Name = output.Name,
        UserCount = output.UserCount
    };

    /// <summary>
    /// Maps an <see cref="AssignRoleRequest"/> to an <see cref="AssignRoleInput"/>.
    /// </summary>
    public static AssignRoleInput ToInput(this AssignRoleRequest request) => new(request.Role);
}
