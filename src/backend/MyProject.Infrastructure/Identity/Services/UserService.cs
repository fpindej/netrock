using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyProject.Application.Caching;
using MyProject.Application.Caching.Constants;
using MyProject.Application.Cookies;
using MyProject.Application.Cookies.Constants;
using MyProject.Application.Features.Audit;
using MyProject.Application.Features.Avatar;
using MyProject.Application.Features.Authentication.Dtos;
using MyProject.Application.Features.FileStorage;
using MyProject.Application.Features.FileStorage.Dtos;
using MyProject.Application.Identity;
using MyProject.Application.Identity.Constants;
using MyProject.Application.Identity.Dtos;
using MyProject.Infrastructure.Features.Authentication.Models;
using MyProject.Infrastructure.Persistence;
using MyProject.Shared;

namespace MyProject.Infrastructure.Identity.Services;

/// <summary>
/// Identity-backed implementation of <see cref="IUserService"/> with Redis caching.
/// </summary>
internal class UserService(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    IUserContext userContext,
    ICacheService cacheService,
    MyProjectDbContext dbContext,
    ICookieService cookieService,
    IAuditService auditService,
    IFileStorageService fileStorageService,
    IImageProcessingService imageProcessingService) : IUserService
{
    private static readonly CacheEntryOptions UserCacheOptions =
        CacheEntryOptions.AbsoluteExpireIn(TimeSpan.FromMinutes(1));

    /// <inheritdoc />
    public async Task<Result<UserOutput>> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var userId = userContext.UserId;

        if (!userId.HasValue)
        {
            return Result<UserOutput>.Failure(ErrorMessages.User.NotAuthenticated, ErrorType.Unauthorized);
        }

        var cacheKey = CacheKeys.User(userId.Value);
        var cachedUser = await cacheService.GetAsync<UserOutput>(cacheKey);

        if (cachedUser is not null)
        {
            return Result<UserOutput>.Success(cachedUser);
        }

        var user = await userManager.FindByIdAsync(userId.Value.ToString());

        if (user is null)
        {
            return Result<UserOutput>.Failure(ErrorMessages.User.NotFound);
        }

        var roles = await userManager.GetRolesAsync(user);
        var permissions = await GetPermissionsForRolesAsync(roles);

        var output = new UserOutput(
            Id: user.Id,
            UserName: user.UserName!,
            FirstName: user.FirstName,
            LastName: user.LastName,
            PhoneNumber: user.PhoneNumber,
            Bio: user.Bio,
            HasAvatar: user.HasAvatar,
            Roles: roles,
            Permissions: permissions,
            IsEmailConfirmed: user.EmailConfirmed);

        // NOTE: UserOutput (including roles and permissions) is cached to improve performance.
        // Role or permission changes may take up to this duration to be reflected.
        await cacheService.SetAsync(cacheKey, output, UserCacheOptions);

        return Result<UserOutput>.Success(output);
    }

    /// <inheritdoc />
    public async Task<IList<string>> GetUserRolesAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return new List<string>();
        }
        return await userManager.GetRolesAsync(user);
    }

    /// <inheritdoc />
    public async Task<Result<UserOutput>> UpdateProfileAsync(UpdateProfileInput input, CancellationToken cancellationToken = default)
    {
        var userId = userContext.UserId;

        if (!userId.HasValue)
        {
            return Result<UserOutput>.Failure(ErrorMessages.User.NotAuthenticated, ErrorType.Unauthorized);
        }

        var user = await userManager.FindByIdAsync(userId.Value.ToString());

        if (user is null)
        {
            return Result<UserOutput>.Failure(ErrorMessages.User.NotFound);
        }

        var normalizedPhone = PhoneNumberHelper.Normalize(input.PhoneNumber);

        if (normalizedPhone is not null && await IsPhoneNumberTakenAsync(normalizedPhone, excludeUserId: userId.Value))
        {
            return Result<UserOutput>.Failure(ErrorMessages.User.PhoneNumberTaken);
        }

        user.FirstName = input.FirstName;
        user.LastName = input.LastName;
        user.PhoneNumber = normalizedPhone;
        user.Bio = input.Bio;

        var result = await userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result<UserOutput>.Failure(errors);
        }

        // Invalidate cache after update
        var cacheKey = CacheKeys.User(userId.Value);
        await cacheService.RemoveAsync(cacheKey);

        var roles = await userManager.GetRolesAsync(user);
        var permissions = await GetPermissionsForRolesAsync(roles);

        var output = new UserOutput(
            Id: user.Id,
            UserName: user.UserName!,
            FirstName: user.FirstName,
            LastName: user.LastName,
            PhoneNumber: user.PhoneNumber,
            Bio: user.Bio,
            HasAvatar: user.HasAvatar,
            Roles: roles,
            Permissions: permissions,
            IsEmailConfirmed: user.EmailConfirmed);

        await auditService.LogAsync(AuditActions.ProfileUpdate, userId: userId.Value, ct: cancellationToken);

        return Result<UserOutput>.Success(output);
    }

    /// <inheritdoc />
    public async Task<Result<UserOutput>> UploadAvatarAsync(byte[] imageData, string fileName, CancellationToken ct)
    {
        var userId = userContext.UserId;

        if (!userId.HasValue)
        {
            return Result<UserOutput>.Failure(ErrorMessages.User.NotAuthenticated, ErrorType.Unauthorized);
        }

        var user = await userManager.FindByIdAsync(userId.Value.ToString());

        if (user is null)
        {
            return Result<UserOutput>.Failure(ErrorMessages.User.NotFound);
        }

        var processResult = imageProcessingService.ProcessAvatar(imageData, fileName);
        if (!processResult.IsSuccess)
        {
            return Result<UserOutput>.Failure(processResult.Error);
        }

        var processed = processResult.Value;
        var storageKey = $"avatars/{userId.Value}.webp";

        var uploadResult = await fileStorageService.UploadAsync(storageKey, processed.ImageData, processed.ContentType, ct);
        if (!uploadResult.IsSuccess)
        {
            return Result<UserOutput>.Failure(uploadResult.Error);
        }

        user.HasAvatar = true;
        await userManager.UpdateAsync(user);

        await InvalidateUserCache(userId.Value);
        await auditService.LogAsync(AuditActions.AvatarUpload, userId: userId.Value, ct: ct);

        return await GetCurrentUserAsync(ct);
    }

    /// <inheritdoc />
    public async Task<Result<UserOutput>> RemoveAvatarAsync(CancellationToken ct)
    {
        var userId = userContext.UserId;

        if (!userId.HasValue)
        {
            return Result<UserOutput>.Failure(ErrorMessages.User.NotAuthenticated, ErrorType.Unauthorized);
        }

        var user = await userManager.FindByIdAsync(userId.Value.ToString());

        if (user is null)
        {
            return Result<UserOutput>.Failure(ErrorMessages.User.NotFound);
        }

        var storageKey = $"avatars/{userId.Value}.webp";
        await fileStorageService.DeleteAsync(storageKey, ct);

        user.HasAvatar = false;
        await userManager.UpdateAsync(user);

        await InvalidateUserCache(userId.Value);
        await auditService.LogAsync(AuditActions.AvatarRemove, userId: userId.Value, ct: ct);

        return await GetCurrentUserAsync(ct);
    }

    /// <inheritdoc />
    public async Task<Result<FileDownloadOutput>> GetAvatarAsync(Guid userId, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null || !user.HasAvatar)
        {
            return Result<FileDownloadOutput>.Failure(ErrorMessages.Avatar.NotFound, ErrorType.NotFound);
        }

        var storageKey = $"avatars/{userId}.webp";
        return await fileStorageService.DownloadAsync(storageKey, ct);
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAccountAsync(DeleteAccountInput input, CancellationToken cancellationToken = default)
    {
        var userId = userContext.UserId;

        if (!userId.HasValue)
        {
            return Result.Failure(ErrorMessages.User.NotAuthenticated, ErrorType.Unauthorized);
        }

        var user = await userManager.FindByIdAsync(userId.Value.ToString());

        if (user is null)
        {
            return Result.Failure(ErrorMessages.User.NotFound);
        }

        var passwordValid = await userManager.CheckPasswordAsync(user, input.Password);

        if (!passwordValid)
        {
            return Result.Failure(ErrorMessages.User.DeleteInvalidPassword);
        }

        var lastAdminResult = await EnforceLastAdminProtectionForDeletionAsync(user, cancellationToken);
        if (!lastAdminResult.IsSuccess)
        {
            return lastAdminResult;
        }

        await auditService.LogAsync(AuditActions.AccountDeletion, userId: userId.Value, ct: cancellationToken);

        // Clean up avatar from storage if present
        if (user.HasAvatar)
        {
            await fileStorageService.DeleteAsync($"avatars/{userId.Value}.webp", cancellationToken);
        }

        await RevokeUserTokens(user, userId.Value, cancellationToken);
        await DeleteUser(user);
        ClearAuthCookies();
        await InvalidateUserCache(userId.Value);

        return Result.Success();
    }

    /// <summary>
    /// Prevents self-deletion if the user is the last holder of any administrative role.
    /// </summary>
    private async Task<Result> EnforceLastAdminProtectionForDeletionAsync(
        ApplicationUser user, CancellationToken cancellationToken)
    {
        var userRoles = await userManager.GetRolesAsync(user);

        foreach (var role in userRoles.Where(r => r is AppRoles.Admin or AppRoles.SuperAdmin))
        {
            var roleEntity = await roleManager.FindByNameAsync(role);
            if (roleEntity is null) continue;

            var usersInRoleCount = await dbContext.UserRoles
                .CountAsync(ur => ur.RoleId == roleEntity.Id, cancellationToken);

            if (usersInRoleCount <= 1)
            {
                return Result.Failure(
                    $"Cannot delete your account — you are the last user with the '{role}' role.");
            }
        }

        return Result.Success();
    }

    private async Task RevokeUserTokens(ApplicationUser user, Guid userId, CancellationToken cancellationToken)
    {
        var tokens = await dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsInvalidated)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.IsInvalidated = true;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await userManager.UpdateSecurityStampAsync(user);
        await cacheService.RemoveAsync(CacheKeys.SecurityStamp(userId), cancellationToken);
    }

    private void ClearAuthCookies()
    {
        cookieService.DeleteCookie(CookieNames.AccessToken);
        cookieService.DeleteCookie(CookieNames.RefreshToken);
    }

    private async Task InvalidateUserCache(Guid userId)
    {
        var cacheKey = CacheKeys.User(userId);
        await cacheService.RemoveAsync(cacheKey);
    }

    private async Task DeleteUser(ApplicationUser user)
    {
        var result = await userManager.DeleteAsync(user);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to delete user account: {errors}");
        }
    }

    /// <summary>
    /// Collects deduplicated permission values for the given roles in a single query.
    /// SuperAdmin receives all permissions implicitly.
    /// </summary>
    private async Task<IReadOnlyList<string>> GetPermissionsForRolesAsync(IList<string> roleNames)
    {
        if (roleNames.Contains(AppRoles.SuperAdmin))
        {
            return AppPermissions.All;
        }

        var normalizedNames = roleNames
            .Select(r => r.ToUpperInvariant())
            .ToList();

        return await dbContext.RoleClaims
            .Join(dbContext.Roles,
                rc => rc.RoleId,
                r => r.Id,
                (rc, r) => new { r.NormalizedName, rc.ClaimType, rc.ClaimValue })
            .Where(x => normalizedNames.Contains(x.NormalizedName!)
                        && x.ClaimType == AppPermissions.ClaimType)
            .Select(x => x.ClaimValue!)
            .Distinct()
            .ToListAsync();
    }

    /// <summary>
    /// Checks whether any existing user already has the given normalized phone number.
    /// </summary>
    private async Task<bool> IsPhoneNumberTakenAsync(string normalizedPhone, Guid excludeUserId)
    {
        return await userManager.Users
            .AnyAsync(u =>
                u.PhoneNumber != null
                && u.PhoneNumber == normalizedPhone
                && u.Id != excludeUserId);
    }
}
