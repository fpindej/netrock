using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
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
/// Identity-backed implementation of <see cref="IUserService"/> with HybridCache caching.
/// </summary>
internal sealed class UserService(
    UserManager<ApplicationUser> userManager,
    IUserContext userContext,
    HybridCache hybridCache,
    MyProjectDbContext dbContext,
    ICookieService cookieService,
    IAuditService auditService,
    IFileStorageService fileStorageService,
    IImageProcessingService imageProcessingService,
    ILogger<UserService> logger) : IUserService
{
    private static readonly HybridCacheEntryOptions UserCacheOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(1)
    };

    /// <inheritdoc />
    public async Task<Result<UserOutput>> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var userId = userContext.UserId;

        if (!userId.HasValue)
        {
            return Result<UserOutput>.Failure(ErrorMessages.User.NotAuthenticated, ErrorType.Unauthorized);
        }

        var cacheKey = CacheKeys.User(userId.Value);

        var output = await hybridCache.GetOrCreateAsync<UserOutput?>(
            cacheKey,
            async ct =>
            {
                var user = await userManager.FindByIdAsync(userId.Value.ToString());

                if (user is null)
                {
                    return null;
                }

                var roles = await userManager.GetRolesAsync(user);
                var permissions = await GetPermissionsForRolesAsync(roles, ct);
                var logins = await userManager.GetLoginsAsync(user);
                var hasPassword = await userManager.HasPasswordAsync(user);

                return new UserOutput(
                    Id: user.Id,
                    UserName: user.UserName!,
                    FirstName: user.FirstName,
                    LastName: user.LastName,
                    PhoneNumber: user.PhoneNumber,
                    Bio: user.Bio,
                    HasAvatar: user.HasAvatar,
                    Roles: roles,
                    Permissions: permissions,
                    IsEmailConfirmed: user.EmailConfirmed,
                    IsTwoFactorEnabled: user.TwoFactorEnabled,
                    LinkedProviders: logins.Select(l => l.LoginProvider).ToList(),
                    HasPassword: hasPassword);
            },
            UserCacheOptions,
            cancellationToken: cancellationToken);

        return output is not null
            ? Result<UserOutput>.Success(output)
            : Result<UserOutput>.Failure(ErrorMessages.User.NotFound);
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
            logger.LogWarning("UpdateAsync failed for user '{UserId}': {Errors}",
                userId.Value, string.Join(", ", result.Errors.Select(e => e.Description)));
            return Result<UserOutput>.Failure(ErrorMessages.User.UpdateFailed);
        }

        // Invalidate cache after update
        var cacheKey = CacheKeys.User(userId.Value);
        await hybridCache.RemoveAsync(cacheKey, cancellationToken);

        var roles = await userManager.GetRolesAsync(user);
        var permissions = await GetPermissionsForRolesAsync(roles, cancellationToken);
        var logins = await userManager.GetLoginsAsync(user);
        var hasPassword = await userManager.HasPasswordAsync(user);

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
            IsEmailConfirmed: user.EmailConfirmed,
            IsTwoFactorEnabled: user.TwoFactorEnabled,
            LinkedProviders: logins.Select(l => l.LoginProvider).ToList(),
            HasPassword: hasPassword);

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
            return Result<UserOutput>.Failure(processResult.Error ?? ErrorMessages.Avatar.ProcessingFailed);
        }

        var processed = processResult.Value;
        var storageKey = $"avatars/{userId.Value}.webp";

        var uploadResult = await fileStorageService.UploadAsync(storageKey, processed.ImageData, processed.ContentType, ct);
        if (!uploadResult.IsSuccess)
        {
            return Result<UserOutput>.Failure(uploadResult.Error ?? ErrorMessages.Avatar.ProcessingFailed);
        }

        user.HasAvatar = true;
        var updateResult = await userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            logger.LogError("Failed to update HasAvatar flag for user {UserId}: {Errors}",
                userId.Value, string.Join(", ", updateResult.Errors.Select(e => e.Description)));
            return Result<UserOutput>.Failure(ErrorMessages.Avatar.ProcessingFailed);
        }

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
        var deleteResult = await fileStorageService.DeleteAsync(storageKey, ct);

        if (!deleteResult.IsSuccess)
        {
            logger.LogWarning("Failed to delete avatar from storage for user {UserId}: {Error}",
                userId.Value, deleteResult.Error);
        }

        user.HasAvatar = false;
        var updateResult = await userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            logger.LogError("Failed to clear HasAvatar flag for user {UserId}: {Errors}",
                userId.Value, string.Join(", ", updateResult.Errors.Select(e => e.Description)));
            return Result<UserOutput>.Failure(ErrorMessages.Avatar.ProcessingFailed);
        }

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

        // Lockout invariant: self-deletion may not leave zero grants-all holders. The flag
        // is captured before the mutation so the in-transaction re-check below still fires
        // after the user's role assignments are gone.
        var userHeldGrantsAll = await UserHoldsGrantsAllRoleAsync(userId.Value, cancellationToken);
        if (userHeldGrantsAll && !await OtherGrantsAllHolderExistsAsync(userId.Value, cancellationToken))
        {
            return Result.Failure(ErrorMessages.User.LastSuperuserCannotDelete);
        }

        // Captured before the mutation: the entity is detached after a committed delete.
        var hadAvatar = user.HasAvatar;

        // Mutation and lockout re-verification share one transaction so two concurrent
        // self-deletions of the last two grants-all holders cannot both slip past the
        // pre-check and jointly leave zero holders. Serializable is required: at READ
        // COMMITTED a reader neither blocks on nor sees a concurrent uncommitted delete of
        // a different row, so both re-checks could still pass. Under serializable isolation
        // Postgres aborts one transaction with a serialization failure, which Npgsql marks
        // transient; the retrying execution strategy re-runs the delegate and the re-check
        // then fails with the stable error code. The InMemory test provider is not
        // relational and ignores transactions, hence the provider guard.
        var deletionResult = Result.Success();
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = dbContext.Database.IsRelational()
                ? await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
                : await dbContext.Database.BeginTransactionAsync(cancellationToken);

            await DeleteUser(user);

            if (userHeldGrantsAll && !await OtherGrantsAllHolderExistsAsync(userId.Value, cancellationToken))
            {
                deletionResult = Result.Failure(ErrorMessages.User.LastSuperuserCannotDelete);
                return;
            }

            await transaction.CommitAsync(cancellationToken);
        });

        if (deletionResult.IsFailure)
        {
            return deletionResult;
        }

        // Side effects run only after a committed delete: on rollback the surviving user
        // must keep sessions and avatar, and no audit record may claim a deletion that
        // never happened.
        await auditService.LogAsync(AuditActions.AccountDeletion, userId: userId.Value, ct: cancellationToken);

        // Clean up avatar from storage if present (best-effort, never blocks the response)
        if (hadAvatar)
        {
            var avatarDeleteResult = await fileStorageService.DeleteAsync($"avatars/{userId.Value}.webp", cancellationToken);
            if (!avatarDeleteResult.IsSuccess)
            {
                logger.LogWarning("Failed to delete avatar for user {UserId} during account deletion: {Error}",
                    userId.Value, avatarDeleteResult.Error);
            }
        }

        // Refresh tokens cascade-delete with the user row, and stamp validation fails
        // closed for a missing user, so evicting the cached security stamp is all that is
        // needed to kill in-flight access tokens. Rotating the stamp via UserManager would
        // issue an update against the deleted row and poison the change tracker.
        await hybridCache.RemoveAsync(CacheKeys.SecurityStamp(userId.Value), cancellationToken);

        ClearAuthCookies();
        await InvalidateUserCache(userId.Value);

        return Result.Success();
    }

    /// <summary>
    /// Determines whether the given user currently holds any role that grants all permissions.
    /// </summary>
    private async Task<bool> UserHoldsGrantsAllRoleAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.UserRoles
            .Where(ur => ur.UserId == userId)
            .Join(dbContext.Roles.Where(r => r.GrantsAllPermissions),
                ur => ur.RoleId,
                r => r.Id,
                (ur, _) => ur.UserId)
            .AnyAsync(cancellationToken);
    }

    /// <summary>
    /// Determines whether any user other than <paramref name="userId"/> holds a role that
    /// grants all permissions.
    /// </summary>
    private async Task<bool> OtherGrantsAllHolderExistsAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.UserRoles
            .Where(ur => ur.UserId != userId)
            .Join(dbContext.Roles.Where(r => r.GrantsAllPermissions),
                ur => ur.RoleId,
                r => r.Id,
                (ur, _) => ur.UserId)
            .AnyAsync(cancellationToken);
    }

    private void ClearAuthCookies()
    {
        cookieService.DeleteCookie(CookieNames.AccessToken);
        cookieService.DeleteCookie(CookieNames.RefreshToken);
    }

    private async Task InvalidateUserCache(Guid userId)
    {
        var cacheKey = CacheKeys.User(userId);
        await hybridCache.RemoveAsync(cacheKey);
    }

    private async Task DeleteUser(ApplicationUser user)
    {
        var result = await userManager.DeleteAsync(user);

        if (!result.Succeeded)
        {
            logger.LogWarning("DeleteAsync failed for user '{UserId}': {Errors}",
                user.Id, string.Join(", ", result.Errors.Select(e => e.Description)));
            throw new InvalidOperationException(ErrorMessages.User.DeleteFailed.Message);
        }
    }

    /// <summary>
    /// Collects deduplicated permission values for the given roles.
    /// Roles that grant all permissions expand to the full permission catalog so API
    /// consumers can keep doing exact membership checks; the wildcard is never exposed.
    /// </summary>
    private async Task<IReadOnlyList<string>> GetPermissionsForRolesAsync(IList<string> roleNames,
        CancellationToken cancellationToken)
    {
        var normalizedNames = roleNames
            .Select(r => r.ToUpperInvariant())
            .ToList();

        var grantsAll = await dbContext.Roles
            .AnyAsync(r => normalizedNames.Contains(r.NormalizedName!) && r.GrantsAllPermissions,
                cancellationToken);

        if (grantsAll)
        {
            return AppPermissions.All;
        }

        return await dbContext.RoleClaims
            .Join(dbContext.Roles,
                rc => rc.RoleId,
                r => r.Id,
                (rc, r) => new { r.NormalizedName, rc.ClaimType, rc.ClaimValue })
            .Where(x => normalizedNames.Contains(x.NormalizedName!)
                        && x.ClaimType == AppPermissions.ClaimType)
            .Select(x => x.ClaimValue!)
            .Distinct()
            .ToListAsync(cancellationToken);
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
