using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyProject.Application.Caching;
using MyProject.Application.Caching.Constants;
using MyProject.Application.Features.Admin;
using MyProject.Application.Features.Admin.Dtos;
using MyProject.Application.Features.Audit;
using MyProject.Application.Features.Email;
using MyProject.Application.Features.Email.Models;
using MyProject.Application.Features.FileStorage;
using MyProject.Application.Identity.Constants;
using MyProject.Infrastructure.Features.Authentication.Models;
using MyProject.Infrastructure.Features.Authentication.Options;
using MyProject.Infrastructure.Features.Authentication.Services;
using MyProject.Infrastructure.Features.Email.Options;
using MyProject.Infrastructure.Persistence;
using MyProject.Infrastructure.Persistence.Extensions;
using MyProject.Shared;

namespace MyProject.Infrastructure.Features.Admin.Services;

/// <summary>
/// Identity-backed implementation of <see cref="IAdminService"/> for administrative user and role management.
/// <para>
/// All mutation operations enforce role hierarchy: the caller must have a strictly higher role rank
/// than the target user. Self-action protection and last-admin guards are applied at this layer
/// to ensure consistent enforcement regardless of the consumer (controller, background job, etc.).
/// </para>
/// <para>
/// Destructive actions (lock, role removal, deletion) revoke all active refresh tokens for the
/// affected user and rotate their security stamp to invalidate in-flight access tokens.
/// </para>
/// </summary>
internal class AdminService(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    MyProjectDbContext dbContext,
    ICacheService cacheService,
    TimeProvider timeProvider,
    IEmailService emailService,
    IEmailTemplateRenderer emailTemplateRenderer,
    EmailTokenService emailTokenService,
    IAuditService auditService,
    IFileStorageService fileStorageService,
    IOptions<AuthenticationOptions> authenticationOptions,
    IOptions<EmailOptions> emailOptions,
    ILogger<AdminService> logger) : IAdminService
{
    private readonly EmailOptions _emailOptions = emailOptions.Value;
    private readonly AuthenticationOptions.EmailTokenOptions _emailTokenOptions = authenticationOptions.Value.EmailToken;

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

        var userOutputs = await MapUsersToOutputsAsync(users, cancellationToken);

        return new AdminUserListOutput(userOutputs, totalCount, pageNumber, pageSize);
    }

    /// <inheritdoc />
    public async Task<Result<AdminUserOutput>> GetUserByIdAsync(Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return Result<AdminUserOutput>.Failure(ErrorMessages.Admin.UserNotFound, ErrorType.NotFound);
        }

        var output = await MapUserToOutputAsync(user, cancellationToken);
        return Result<AdminUserOutput>.Success(output);
    }

    /// <inheritdoc />
    public async Task<Result> AssignRoleAsync(Guid callerUserId, Guid userId, AssignRoleInput input,
        CancellationToken cancellationToken = default)
    {
        var roleExists = await roleManager.FindByNameAsync(input.Role) is not null;
        if (!roleExists)
        {
            return Result.Failure(ErrorMessages.Admin.RoleNotFound);
        }

        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return Result.Failure(ErrorMessages.Admin.UserNotFound, ErrorType.NotFound);
        }

        var hierarchyResult = await EnforceHierarchyAsync(callerUserId, user);
        if (!hierarchyResult.IsSuccess)
        {
            return hierarchyResult;
        }

        var callerRoles = await GetUserRolesAsync(callerUserId);
        var callerRank = AppRoles.GetHighestRank(callerRoles);

        if (AppRoles.GetRoleRank(input.Role) >= callerRank)
        {
            return Result.Failure(ErrorMessages.Admin.RoleAssignAboveRank);
        }

        if (AppRoles.GetRoleRank(input.Role) == 0)
        {
            var escalationResult = await EnforceRolePermissionEscalationAsync(input.Role, callerRoles);
            if (!escalationResult.IsSuccess)
            {
                return escalationResult;
            }
        }

        if (await userManager.IsInRoleAsync(user, input.Role))
        {
            return Result.Failure(ErrorMessages.Admin.RoleAlreadyAssigned);
        }

        if (AppRoles.GetRoleRank(input.Role) > 0 && !user.EmailConfirmed)
        {
            return Result.Failure(ErrorMessages.Admin.EmailVerificationRequired);
        }

        var result = await userManager.AddToRoleAsync(user, input.Role);

        if (!result.Succeeded)
        {
            logger.LogWarning("AddToRoleAsync failed for user '{UserId}': {Errors}",
                userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return Result.Failure(ErrorMessages.Admin.RoleAssignFailed);
        }

        await RotateSecurityStampAsync(user, userId, cancellationToken);
        await InvalidateUserCacheAsync(userId);
        logger.LogInformation("Role '{Role}' assigned to user '{UserId}' by admin '{CallerUserId}'",
            input.Role, userId, callerUserId);

        await auditService.LogAsync(AuditActions.AdminAssignRole, userId: callerUserId,
            targetEntityType: "User", targetEntityId: userId,
            metadata: JsonSerializer.Serialize(new { role = input.Role }), ct: cancellationToken);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> RemoveRoleAsync(Guid callerUserId, Guid userId, string role,
        CancellationToken cancellationToken = default)
    {
        var roleExists = await roleManager.FindByNameAsync(role) is not null;
        if (!roleExists)
        {
            return Result.Failure(ErrorMessages.Admin.RoleNotFound);
        }

        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return Result.Failure(ErrorMessages.Admin.UserNotFound, ErrorType.NotFound);
        }

        if (callerUserId == userId)
        {
            return Result.Failure(ErrorMessages.Admin.RoleSelfRemove);
        }

        var hierarchyResult = await EnforceHierarchyAsync(callerUserId, user);
        if (!hierarchyResult.IsSuccess)
        {
            return hierarchyResult;
        }

        var callerRoles = await GetUserRolesAsync(callerUserId);
        var callerRank = AppRoles.GetHighestRank(callerRoles);

        if (AppRoles.GetRoleRank(role) >= callerRank)
        {
            return Result.Failure(ErrorMessages.Admin.RoleRemoveAboveRank);
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            return Result.Failure(ErrorMessages.Admin.RoleNotAssigned);
        }

        var lastAdminResult = await EnforceLastAdminProtectionAsync(userId, role, cancellationToken);
        if (!lastAdminResult.IsSuccess)
        {
            return lastAdminResult;
        }

        var result = await userManager.RemoveFromRoleAsync(user, role);

        if (!result.Succeeded)
        {
            logger.LogWarning("RemoveFromRoleAsync failed for user '{UserId}': {Errors}",
                userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return Result.Failure(ErrorMessages.Admin.RoleRemoveFailed);
        }

        await RotateSecurityStampAsync(user, userId, cancellationToken);
        await InvalidateUserCacheAsync(userId);
        logger.LogInformation("Role '{Role}' removed from user '{UserId}' by admin '{CallerUserId}'",
            role, userId, callerUserId);

        await auditService.LogAsync(AuditActions.AdminRemoveRole, userId: callerUserId,
            targetEntityType: "User", targetEntityId: userId,
            metadata: JsonSerializer.Serialize(new { role }), ct: cancellationToken);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> LockUserAsync(Guid callerUserId, Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return Result.Failure(ErrorMessages.Admin.UserNotFound, ErrorType.NotFound);
        }

        if (callerUserId == userId)
        {
            return Result.Failure(ErrorMessages.Admin.LockSelfAction);
        }

        var hierarchyResult = await EnforceHierarchyAsync(callerUserId, user);
        if (!hierarchyResult.IsSuccess)
        {
            return hierarchyResult;
        }

        // Set lockout end to 100 years in the future (effectively permanent)
        var lockoutEnd = timeProvider.GetUtcNow().AddYears(100);
        var result = await userManager.SetLockoutEndDateAsync(user, lockoutEnd);

        if (!result.Succeeded)
        {
            logger.LogWarning("SetLockoutEndDateAsync (lock) failed for user '{UserId}': {Errors}",
                userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return Result.Failure(ErrorMessages.Admin.LockFailed);
        }

        await RevokeUserSessionsAsync(user, userId, cancellationToken);
        await InvalidateUserCacheAsync(userId);
        logger.LogWarning("User '{UserId}' has been locked out by admin '{CallerUserId}'",
            userId, callerUserId);

        await auditService.LogAsync(AuditActions.AdminLockUser, userId: callerUserId,
            targetEntityType: "User", targetEntityId: userId, ct: cancellationToken);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> UnlockUserAsync(Guid callerUserId, Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return Result.Failure(ErrorMessages.Admin.UserNotFound, ErrorType.NotFound);
        }

        var hierarchyResult = await EnforceHierarchyAsync(callerUserId, user);
        if (!hierarchyResult.IsSuccess)
        {
            return hierarchyResult;
        }

        var result = await userManager.SetLockoutEndDateAsync(user, null);

        if (!result.Succeeded)
        {
            logger.LogWarning("SetLockoutEndDateAsync (unlock) failed for user '{UserId}': {Errors}",
                userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return Result.Failure(ErrorMessages.Admin.UnlockFailed);
        }

        // Reset access failed count
        await userManager.ResetAccessFailedCountAsync(user);

        await InvalidateUserCacheAsync(userId);
        logger.LogInformation("User '{UserId}' has been unlocked by admin '{CallerUserId}'",
            userId, callerUserId);

        await auditService.LogAsync(AuditActions.AdminUnlockUser, userId: callerUserId,
            targetEntityType: "User", targetEntityId: userId, ct: cancellationToken);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> DeleteUserAsync(Guid callerUserId, Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return Result.Failure(ErrorMessages.Admin.UserNotFound, ErrorType.NotFound);
        }

        if (callerUserId == userId)
        {
            return Result.Failure(ErrorMessages.Admin.DeleteSelfAction);
        }

        var hierarchyResult = await EnforceHierarchyAsync(callerUserId, user);
        if (!hierarchyResult.IsSuccess)
        {
            return hierarchyResult;
        }

        var targetRoles = await userManager.GetRolesAsync(user);
        var lastAdminCheckResult = await EnforceLastAdminProtectionForDeletionAsync(
            targetRoles, cancellationToken);
        if (!lastAdminCheckResult.IsSuccess)
        {
            return lastAdminCheckResult;
        }

        await RevokeUserSessionsAsync(user, userId, cancellationToken);

        // Clean up avatar from storage if present (best-effort — don't block account deletion)
        if (user.HasAvatar)
        {
            var avatarDeleteResult = await fileStorageService.DeleteAsync($"avatars/{userId}.webp", cancellationToken);
            if (!avatarDeleteResult.IsSuccess)
            {
                logger.LogWarning("Failed to delete avatar for user {UserId} during admin deletion: {Error}",
                    userId, avatarDeleteResult.Error);
            }
        }

        var result = await userManager.DeleteAsync(user);

        if (!result.Succeeded)
        {
            logger.LogWarning("DeleteAsync failed for user '{UserId}': {Errors}",
                userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return Result.Failure(ErrorMessages.Admin.DeleteFailed);
        }

        await InvalidateUserCacheAsync(userId);
        logger.LogWarning("User '{UserId}' has been deleted by admin '{CallerUserId}'",
            userId, callerUserId);

        await auditService.LogAsync(AuditActions.AdminDeleteUser, userId: callerUserId,
            targetEntityType: "User", targetEntityId: userId, ct: cancellationToken);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdminRoleOutput>> GetRolesAsync(
        CancellationToken cancellationToken = default)
    {
        var roles = await roleManager.Roles
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var roleCounts = await dbContext.UserRoles
            .GroupBy(ur => ur.RoleId)
            .Select(g => new { RoleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RoleId, x => x.Count, cancellationToken);

        var roleClaims = (await dbContext.RoleClaims
                .Where(rc => rc.ClaimType == AppPermissions.ClaimType)
                .Select(rc => new { rc.RoleId, rc.ClaimValue })
                .ToListAsync(cancellationToken))
            .GroupBy(rc => rc.RoleId)
            .ToDictionary(g => g.Key, g => g.Select(c => c.ClaimValue!).ToList());

        return roles
            .Select(role => new AdminRoleOutput(
                role.Id,
                role.Name ?? string.Empty,
                role.Description,
                AppRoles.All.Contains(role.Name ?? string.Empty),
                roleCounts.GetValueOrDefault(role.Id),
                roleClaims.GetValueOrDefault(role.Id, [])))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<Result> VerifyEmailAsync(Guid callerUserId, Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return Result.Failure(ErrorMessages.Admin.UserNotFound, ErrorType.NotFound);
        }

        var hierarchyResult = await EnforceHierarchyAsync(callerUserId, user);
        if (!hierarchyResult.IsSuccess)
        {
            return hierarchyResult;
        }

        if (user.EmailConfirmed)
        {
            return Result.Failure(ErrorMessages.Auth.EmailAlreadyVerified);
        }

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var confirmResult = await userManager.ConfirmEmailAsync(user, token);

        if (!confirmResult.Succeeded)
        {
            logger.LogWarning("ConfirmEmailAsync failed for user '{UserId}': {Errors}",
                userId, string.Join(", ", confirmResult.Errors.Select(e => e.Description)));
            return Result.Failure(ErrorMessages.Admin.EmailVerificationFailed);
        }

        await InvalidateUserCacheAsync(userId);
        logger.LogInformation("Email for user '{UserId}' manually verified by admin '{CallerUserId}'",
            userId, callerUserId);

        await auditService.LogAsync(AuditActions.AdminVerifyEmail, userId: callerUserId,
            targetEntityType: "User", targetEntityId: userId, ct: cancellationToken);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> SendPasswordResetAsync(Guid callerUserId, Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return Result.Failure(ErrorMessages.Admin.UserNotFound, ErrorType.NotFound);
        }

        var hierarchyResult = await EnforceHierarchyAsync(callerUserId, user);
        if (!hierarchyResult.IsSuccess)
        {
            return hierarchyResult;
        }

        var identityToken = await userManager.GeneratePasswordResetTokenAsync(user);
        var opaqueToken = await emailTokenService.CreateAsync(user.Id, identityToken, EmailTokenPurpose.PasswordReset, cancellationToken);
        var email = user.Email ?? user.UserName ?? string.Empty;
        var resetUrl = $"{_emailOptions.FrontendBaseUrl.TrimEnd('/')}/reset-password?token={opaqueToken}";

        var model = new AdminResetPasswordModel(resetUrl, _emailTokenOptions.Lifetime.ToHumanReadable());
        await SendTemplatedEmailSafeAsync("admin-reset-password", model, email, cancellationToken);

        logger.LogInformation("Password reset email sent for user '{UserId}' by admin '{CallerUserId}'",
            userId, callerUserId);

        await auditService.LogAsync(AuditActions.AdminSendPasswordReset, userId: callerUserId,
            targetEntityType: "User", targetEntityId: userId, ct: cancellationToken);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> CreateUserAsync(Guid callerUserId, CreateUserInput input,
        CancellationToken cancellationToken = default)
    {
        var existingUser = await userManager.FindByEmailAsync(input.Email);
        if (existingUser is not null)
        {
            return Result<Guid>.Failure(ErrorMessages.Admin.EmailAlreadyRegistered);
        }

        var tempPassword = GenerateTemporaryPassword();

        var user = new ApplicationUser
        {
            UserName = input.Email,
            Email = input.Email,
            EmailConfirmed = true,
            FirstName = input.FirstName,
            LastName = input.LastName,
            LockoutEnabled = true
        };

        var createResult = await userManager.CreateAsync(user, tempPassword);
        if (!createResult.Succeeded)
        {
            logger.LogWarning("CreateAsync failed for admin-created user '{Email}': {Errors}",
                input.Email, string.Join(", ", createResult.Errors.Select(e => e.Description)));
            return Result<Guid>.Failure(ErrorMessages.Admin.CreateUserFailed);
        }

        var roleResult = await userManager.AddToRoleAsync(user, AppRoles.User);
        if (!roleResult.Succeeded)
        {
            logger.LogWarning("User '{UserId}' created but default role assignment failed", user.Id);
        }

        // Send invitation email with password reset link
        var identityToken = await userManager.GeneratePasswordResetTokenAsync(user);
        var opaqueToken = await emailTokenService.CreateAsync(user.Id, identityToken, EmailTokenPurpose.PasswordReset, cancellationToken);
        var setPasswordUrl = $"{_emailOptions.FrontendBaseUrl.TrimEnd('/')}/reset-password?token={opaqueToken}&invited=1";

        var invitationModel = new InvitationModel(setPasswordUrl, _emailTokenOptions.Lifetime.ToHumanReadable());
        await SendTemplatedEmailSafeAsync("invitation", invitationModel, input.Email, cancellationToken);

        logger.LogInformation("User '{UserId}' created via admin invitation for email '{Email}' by admin '{CallerUserId}'",
            user.Id, input.Email, callerUserId);

        await auditService.LogAsync(AuditActions.AdminCreateUser, userId: callerUserId,
            targetEntityType: "User", targetEntityId: user.Id, ct: cancellationToken);

        return Result<Guid>.Success(user.Id);
    }

    /// <summary>
    /// Verifies that the caller has a strictly higher role rank than the target user.
    /// Returns <see cref="Result.Failure(string)"/> if the hierarchy check fails.
    /// </summary>
    private async Task<Result> EnforceHierarchyAsync(Guid callerUserId, ApplicationUser targetUser)
    {
        var callerRoles = await GetUserRolesAsync(callerUserId);
        var targetRoles = await userManager.GetRolesAsync(targetUser);

        var callerRank = AppRoles.GetHighestRank(callerRoles);
        var targetRank = AppRoles.GetHighestRank(targetRoles);

        if (callerRank <= targetRank)
        {
            return Result.Failure(ErrorMessages.Admin.HierarchyInsufficient);
        }

        return Result.Success();
    }

    /// <summary>
    /// Prevents removal of an administrative role if the target user is the last user with that role.
    /// Only applies to Admin and SuperAdmin roles.
    /// </summary>
    private async Task<Result> EnforceLastAdminProtectionAsync(Guid userId, string role,
        CancellationToken cancellationToken)
    {
        if (role is not (AppRoles.Admin or AppRoles.SuperAdmin))
        {
            return Result.Success();
        }

        var roleEntity = await roleManager.FindByNameAsync(role);
        if (roleEntity is null)
        {
            return Result.Success();
        }

        var usersInRoleCount = await dbContext.UserRoles
            .CountAsync(ur => ur.RoleId == roleEntity.Id, cancellationToken);

        if (usersInRoleCount <= 1)
        {
            return Result.Failure(ErrorMessages.Admin.LastRoleHolder);
        }

        return Result.Success();
    }

    /// <summary>
    /// Prevents deletion of a user if they are the last user holding any administrative role
    /// (Admin or SuperAdmin).
    /// </summary>
    private async Task<Result> EnforceLastAdminProtectionForDeletionAsync(
        IList<string> targetRoles, CancellationToken cancellationToken)
    {
        foreach (var role in targetRoles.Where(r => r is AppRoles.Admin or AppRoles.SuperAdmin))
        {
            var roleEntity = await roleManager.FindByNameAsync(role);
            if (roleEntity is null) continue;

            var usersInRoleCount = await dbContext.UserRoles
                .CountAsync(ur => ur.RoleId == roleEntity.Id, cancellationToken);

            if (usersInRoleCount <= 1)
            {
                return Result.Failure(ErrorMessages.Admin.LastAdminCannotDelete);
            }
        }

        return Result.Success();
    }

    /// <summary>
    /// Verifies that the caller holds every permission granted by the target custom role.
    /// SuperAdmin callers are exempt (implicit all permissions).
    /// Roles with no permissions are allowed unconditionally.
    /// </summary>
    private async Task<Result> EnforceRolePermissionEscalationAsync(string roleName, IList<string> callerRoles)
    {
        var targetRole = await roleManager.FindByNameAsync(roleName);
        if (targetRole is null)
        {
            return Result.Success();
        }

        var targetClaims = await roleManager.GetClaimsAsync(targetRole);
        var targetPermissions = targetClaims
            .Where(c => c.Type == AppPermissions.ClaimType)
            .Select(c => c.Value)
            .ToList();

        if (targetPermissions.Count == 0)
        {
            return Result.Success();
        }

        if (callerRoles.Contains(AppRoles.SuperAdmin))
        {
            return Result.Success();
        }

        var callerPermissions = new HashSet<string>(StringComparer.Ordinal);
        foreach (var callerRoleName in callerRoles)
        {
            var callerRole = await roleManager.FindByNameAsync(callerRoleName);
            if (callerRole is null) continue;

            var claims = await roleManager.GetClaimsAsync(callerRole);
            foreach (var claim in claims.Where(c => c.Type == AppPermissions.ClaimType))
            {
                callerPermissions.Add(claim.Value);
            }
        }

        if (!targetPermissions.All(callerPermissions.Contains))
        {
            return Result.Failure(ErrorMessages.Admin.RoleAssignEscalation, ErrorType.Forbidden);
        }

        return Result.Success();
    }

    /// <summary>
    /// Rotates a user's security stamp, invalidating their current access token.
    /// Refresh tokens are preserved so the frontend can silently re-authenticate
    /// and obtain a new JWT with updated claims.
    /// </summary>
    private async Task RotateSecurityStampAsync(ApplicationUser user, Guid userId,
        CancellationToken cancellationToken)
    {
        await userManager.UpdateSecurityStampAsync(user);
        await cacheService.RemoveAsync(CacheKeys.SecurityStamp(userId), cancellationToken);
    }

    /// <summary>
    /// Revokes all active refresh tokens for a user and rotates their security stamp,
    /// forcing re-authentication on all devices.
    /// </summary>
    private async Task RevokeUserSessionsAsync(ApplicationUser user, Guid userId,
        CancellationToken cancellationToken)
    {
        var tokens = await dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsInvalidated)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.IsInvalidated = true;
        }

        if (tokens.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await userManager.UpdateSecurityStampAsync(user);
        await cacheService.RemoveAsync(CacheKeys.SecurityStamp(userId), cancellationToken);
    }

    private async Task<IList<string>> GetUserRolesAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return [];
        }

        return await userManager.GetRolesAsync(user);
    }

    private async Task<IReadOnlyList<AdminUserOutput>> MapUsersToOutputsAsync(
        IReadOnlyList<ApplicationUser> users, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        var userIds = users.Select(u => u.Id).ToList();

        var userRolesMap = await dbContext.UserRoles
            .Where(ur => userIds.Contains(ur.UserId))
            .Join(dbContext.Roles, ur => ur.RoleId, r => r.Id,
                (ur, r) => new { ur.UserId, RoleName = r.Name ?? string.Empty })
            .GroupBy(x => x.UserId)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.Select(x => x.RoleName).ToList(),
                cancellationToken);

        return users.Select(user =>
        {
            var roles = userRolesMap.GetValueOrDefault(user.Id, []);
            var isLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd > now;

            return new AdminUserOutput(
                Id: user.Id,
                UserName: user.UserName ?? string.Empty,
                FirstName: user.FirstName,
                LastName: user.LastName,
                PhoneNumber: user.PhoneNumber,
                Bio: user.Bio,
                HasAvatar: user.HasAvatar,
                Roles: roles,
                EmailConfirmed: user.EmailConfirmed,
                LockoutEnabled: user.LockoutEnabled,
                LockoutEnd: user.LockoutEnd,
                AccessFailedCount: user.AccessFailedCount,
                IsLockedOut: isLockedOut);
        }).ToList();
    }

    private async Task<AdminUserOutput> MapUserToOutputAsync(ApplicationUser user,
        CancellationToken cancellationToken)
    {
        var roleNames = await dbContext.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Join(dbContext.Roles, ur => ur.RoleId, r => r.Id,
                (_, r) => r.Name ?? string.Empty)
            .ToListAsync(cancellationToken);

        var now = timeProvider.GetUtcNow();
        var isLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd > now;

        return new AdminUserOutput(
            Id: user.Id,
            UserName: user.UserName ?? string.Empty,
            FirstName: user.FirstName,
            LastName: user.LastName,
            PhoneNumber: user.PhoneNumber,
            Bio: user.Bio,
            HasAvatar: user.HasAvatar,
            Roles: roleNames,
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

    /// <summary>
    /// Renders a templated email and sends it, swallowing both rendering and delivery failures.
    /// Transient provider outages (quota, auth, network) and template errors are logged
    /// but never propagate to the caller.
    /// </summary>
    private async Task SendTemplatedEmailSafeAsync<TModel>(
        string templateName, TModel model, string to, CancellationToken cancellationToken) where TModel : class
    {
        try
        {
            var rendered = emailTemplateRenderer.Render(templateName, model);
            var message = new EmailMessage(to, rendered.Subject, rendered.HtmlBody, rendered.PlainTextBody);
            await emailService.SendEmailAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send templated email '{TemplateName}' to {To}", templateName, to);
        }
    }

    /// <summary>
    /// Generates a cryptographically random temporary password that satisfies default ASP.NET Identity complexity rules.
    /// </summary>
    private static string GenerateTemporaryPassword()
    {
        const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lower = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string special = "!@#$%^&*";
        const string all = upper + lower + digits + special;

        Span<byte> randomBytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(randomBytes);

        var password = new char[32];
        // Ensure at least one of each required category
        password[0] = upper[randomBytes[0] % upper.Length];
        password[1] = lower[randomBytes[1] % lower.Length];
        password[2] = digits[randomBytes[2] % digits.Length];
        password[3] = special[randomBytes[3] % special.Length];

        for (var i = 4; i < 32; i++)
        {
            password[i] = all[randomBytes[i] % all.Length];
        }

        // Shuffle to avoid predictable prefix
        Span<byte> shuffleBytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(shuffleBytes);
        for (var i = password.Length - 1; i > 0; i--)
        {
            var j = shuffleBytes[i] % (i + 1);
            (password[i], password[j]) = (password[j], password[i]);
        }

        return new string(password);
    }
}
