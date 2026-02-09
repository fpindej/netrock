using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyProject.Application.Caching;
using MyProject.Application.Caching.Constants;
using MyProject.Application.Features.Admin;
using MyProject.Application.Features.Admin.Dtos;
using MyProject.Application.Identity.Constants;
using MyProject.Domain;
using MyProject.Infrastructure.Features.Authentication.Models;
using MyProject.Infrastructure.Persistence;
using MyProject.Infrastructure.Persistence.Extensions;

namespace MyProject.Infrastructure.Features.Admin.Services;

/// <summary>
/// Identity-backed implementation of <see cref="IAdminService"/> for administrative user and role management.
/// </summary>
internal class AdminService(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    MyProjectDbContext dbContext,
    ICacheService cacheService,
    TimeProvider timeProvider,
    ILogger<AdminService> logger) : IAdminService
{
    /// <inheritdoc />
    public async Task<AdminUserListOutput> GetUsersAsync(int pageNumber, int pageSize, string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLowerInvariant();
            query = query.Where(u =>
                (u.UserName != null && u.UserName.ToLower().Contains(searchLower)) ||
                (u.FirstName != null && u.FirstName.ToLower().Contains(searchLower)) ||
                (u.LastName != null && u.LastName.ToLower().Contains(searchLower)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderBy(u => u.UserName)
            .Paginate(pageNumber, pageSize)
            .ToListAsync(cancellationToken);

        var userOutputs = await MapUsersToOutputsAsync(users);

        return new AdminUserListOutput(userOutputs, totalCount, pageNumber, pageSize);
    }

    /// <inheritdoc />
    public async Task<Result<AdminUserOutput>> GetUserByIdAsync(Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            throw new KeyNotFoundException($"User with ID '{userId}' was not found.");
        }

        var output = await MapUserToOutputAsync(user);
        return Result<AdminUserOutput>.Success(output);
    }

    /// <inheritdoc />
    public async Task<Result> AssignRoleAsync(Guid userId, AssignRoleInput input,
        CancellationToken cancellationToken = default)
    {
        if (!AppRoles.All.Contains(input.Role))
        {
            return Result.Failure($"Role '{input.Role}' does not exist.");
        }

        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            throw new KeyNotFoundException($"User with ID '{userId}' was not found.");
        }

        if (await userManager.IsInRoleAsync(user, input.Role))
        {
            return Result.Failure($"User already has the '{input.Role}' role.");
        }

        var result = await userManager.AddToRoleAsync(user, input.Role);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure(errors);
        }

        await InvalidateUserCacheAsync(userId);
        logger.LogInformation("Role '{Role}' assigned to user '{UserId}'", input.Role, userId);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> RemoveRoleAsync(Guid userId, string role,
        CancellationToken cancellationToken = default)
    {
        if (!AppRoles.All.Contains(role))
        {
            return Result.Failure($"Role '{role}' does not exist.");
        }

        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            throw new KeyNotFoundException($"User with ID '{userId}' was not found.");
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            return Result.Failure($"User does not have the '{role}' role.");
        }

        var result = await userManager.RemoveFromRoleAsync(user, role);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure(errors);
        }

        await InvalidateUserCacheAsync(userId);
        logger.LogInformation("Role '{Role}' removed from user '{UserId}'", role, userId);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> LockUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            throw new KeyNotFoundException($"User with ID '{userId}' was not found.");
        }

        // Set lockout end to 100 years in the future (effectively permanent)
        var lockoutEnd = timeProvider.GetUtcNow().AddYears(100);
        var result = await userManager.SetLockoutEndDateAsync(user, lockoutEnd);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure(errors);
        }

        await InvalidateUserCacheAsync(userId);
        logger.LogWarning("User '{UserId}' has been locked out by admin", userId);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> UnlockUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            throw new KeyNotFoundException($"User with ID '{userId}' was not found.");
        }

        var result = await userManager.SetLockoutEndDateAsync(user, null);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure(errors);
        }

        // Reset access failed count
        await userManager.ResetAccessFailedCountAsync(user);

        await InvalidateUserCacheAsync(userId);
        logger.LogInformation("User '{UserId}' has been unlocked by admin", userId);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            throw new KeyNotFoundException($"User with ID '{userId}' was not found.");
        }

        var result = await userManager.DeleteAsync(user);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure(errors);
        }

        await InvalidateUserCacheAsync(userId);
        logger.LogWarning("User '{UserId}' has been deleted by admin", userId);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdminRoleOutput>> GetRolesAsync(
        CancellationToken cancellationToken = default)
    {
        var roles = await roleManager.Roles
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var roleOutputs = new List<AdminRoleOutput>(roles.Count);

        foreach (var role in roles)
        {
            var userCount = await dbContext.UserRoles
                .CountAsync(ur => ur.RoleId == role.Id, cancellationToken);

            roleOutputs.Add(new AdminRoleOutput(role.Id, role.Name!, userCount));
        }

        return roleOutputs;
    }

    private async Task<IReadOnlyList<AdminUserOutput>> MapUsersToOutputsAsync(
        IReadOnlyList<ApplicationUser> users)
    {
        var outputs = new List<AdminUserOutput>(users.Count);
        var now = timeProvider.GetUtcNow();

        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            var isLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd > now;

            outputs.Add(new AdminUserOutput(
                Id: user.Id,
                UserName: user.UserName!,
                FirstName: user.FirstName,
                LastName: user.LastName,
                PhoneNumber: user.PhoneNumber,
                Bio: user.Bio,
                AvatarUrl: user.AvatarUrl,
                Roles: roles.ToList(),
                EmailConfirmed: user.EmailConfirmed,
                LockoutEnabled: user.LockoutEnabled,
                LockoutEnd: user.LockoutEnd,
                AccessFailedCount: user.AccessFailedCount,
                IsLockedOut: isLockedOut));
        }

        return outputs;
    }

    private async Task<AdminUserOutput> MapUserToOutputAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        var now = timeProvider.GetUtcNow();
        var isLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd > now;

        return new AdminUserOutput(
            Id: user.Id,
            UserName: user.UserName!,
            FirstName: user.FirstName,
            LastName: user.LastName,
            PhoneNumber: user.PhoneNumber,
            Bio: user.Bio,
            AvatarUrl: user.AvatarUrl,
            Roles: roles.ToList(),
            EmailConfirmed: user.EmailConfirmed,
            LockoutEnabled: user.LockoutEnabled,
            LockoutEnd: user.LockoutEnd,
            AccessFailedCount: user.AccessFailedCount,
            IsLockedOut: isLockedOut);
    }

    private async Task InvalidateUserCacheAsync(Guid userId)
    {
        await cacheService.RemoveAsync(CacheKeys.User(userId));
    }
}
