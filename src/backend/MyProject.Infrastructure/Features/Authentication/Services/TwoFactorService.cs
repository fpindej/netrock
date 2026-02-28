using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using MyProject.Application.Features.Audit;
using MyProject.Application.Features.Authentication;
using MyProject.Application.Features.Authentication.Dtos;
using MyProject.Application.Identity;
using MyProject.Infrastructure.Features.Authentication.Models;
using MyProject.Shared;

namespace MyProject.Infrastructure.Features.Authentication.Services;

/// <summary>
/// Identity-backed implementation of <see cref="ITwoFactorService"/> for TOTP two-factor authentication management.
/// </summary>
internal class TwoFactorService(
    UserManager<ApplicationUser> userManager,
    IUserContext userContext,
    IAuditService auditService,
    ILogger<TwoFactorService> logger) : ITwoFactorService
{
    private const int RecoveryCodeCount = 10;

    /// <inheritdoc />
    public async Task<Result<TwoFactorSetupOutput>> SetupAsync(CancellationToken ct)
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
        {
            return Result<TwoFactorSetupOutput>.Failure(ErrorMessages.Auth.UserNotFound, ErrorType.Unauthorized);
        }

        if (user.TwoFactorEnabled)
        {
            return Result<TwoFactorSetupOutput>.Failure(ErrorMessages.TwoFactor.AlreadyEnabled);
        }

        await userManager.ResetAuthenticatorKeyAsync(user);
        var key = await userManager.GetAuthenticatorKeyAsync(user);

        if (string.IsNullOrEmpty(key))
        {
            logger.LogError("Failed to generate authenticator key for user {UserId}", user.Id);
            return Result<TwoFactorSetupOutput>.Failure(ErrorMessages.TwoFactor.SetupFailed);
        }

        var email = user.Email ?? user.UserName ?? "user";
        var uri = GenerateOtpAuthUri(email, key);

        return Result<TwoFactorSetupOutput>.Success(new TwoFactorSetupOutput(key, uri));
    }

    /// <inheritdoc />
    public async Task<Result<TwoFactorVerifySetupOutput>> VerifySetupAsync(string code, CancellationToken ct)
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
        {
            return Result<TwoFactorVerifySetupOutput>.Failure(ErrorMessages.Auth.UserNotFound, ErrorType.Unauthorized);
        }

        if (user.TwoFactorEnabled)
        {
            return Result<TwoFactorVerifySetupOutput>.Failure(ErrorMessages.TwoFactor.AlreadyEnabled);
        }

        var isValid = await userManager.VerifyTwoFactorTokenAsync(
            user, TokenOptions.DefaultAuthenticatorProvider, code);

        if (!isValid)
        {
            return Result<TwoFactorVerifySetupOutput>.Failure(ErrorMessages.TwoFactor.VerificationFailed);
        }

        var enableResult = await userManager.SetTwoFactorEnabledAsync(user, true);
        if (!enableResult.Succeeded)
        {
            logger.LogError("Failed to enable 2FA for user {UserId}: {Errors}",
                user.Id, string.Join(", ", enableResult.Errors.Select(e => e.Description)));
            return Result<TwoFactorVerifySetupOutput>.Failure(ErrorMessages.TwoFactor.SetupFailed);
        }

        var codes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, RecoveryCodeCount);

        await auditService.LogAsync(AuditActions.TwoFactorEnabled, userId: user.Id, ct: ct);

        return Result<TwoFactorVerifySetupOutput>.Success(
            new TwoFactorVerifySetupOutput(codes?.ToList() ?? []));
    }

    /// <inheritdoc />
    public async Task<Result> DisableAsync(string password, CancellationToken ct)
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
        {
            return Result.Failure(ErrorMessages.Auth.UserNotFound, ErrorType.Unauthorized);
        }

        if (!user.TwoFactorEnabled)
        {
            return Result.Failure(ErrorMessages.TwoFactor.NotEnabled);
        }

        if (!await userManager.CheckPasswordAsync(user, password))
        {
            return Result.Failure(ErrorMessages.Auth.PasswordIncorrect);
        }

        var disableResult = await userManager.SetTwoFactorEnabledAsync(user, false);
        if (!disableResult.Succeeded)
        {
            logger.LogError("Failed to disable 2FA for user {UserId}: {Errors}",
                user.Id, string.Join(", ", disableResult.Errors.Select(e => e.Description)));
            return Result.Failure(ErrorMessages.TwoFactor.DisableFailed);
        }

        await userManager.ResetAuthenticatorKeyAsync(user);

        await auditService.LogAsync(AuditActions.TwoFactorDisabled, userId: user.Id, ct: ct);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<TwoFactorVerifySetupOutput>> RegenerateRecoveryCodesAsync(string password, CancellationToken ct)
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
        {
            return Result<TwoFactorVerifySetupOutput>.Failure(ErrorMessages.Auth.UserNotFound, ErrorType.Unauthorized);
        }

        if (!user.TwoFactorEnabled)
        {
            return Result<TwoFactorVerifySetupOutput>.Failure(ErrorMessages.TwoFactor.NotEnabled);
        }

        if (!await userManager.CheckPasswordAsync(user, password))
        {
            return Result<TwoFactorVerifySetupOutput>.Failure(ErrorMessages.Auth.PasswordIncorrect);
        }

        var codes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, RecoveryCodeCount);

        await auditService.LogAsync(AuditActions.TwoFactorRecoveryCodesRegenerated, userId: user.Id, ct: ct);

        return Result<TwoFactorVerifySetupOutput>.Success(
            new TwoFactorVerifySetupOutput(codes?.ToList() ?? []));
    }

    /// <summary>
    /// Gets the current authenticated user from Identity.
    /// </summary>
    private async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var userId = userContext.UserId;
        if (!userId.HasValue)
        {
            return null;
        }
        return await userManager.FindByIdAsync(userId.Value.ToString());
    }

    /// <summary>
    /// Generates an otpauth:// URI for QR code scanning by authenticator apps.
    /// </summary>
    private static string GenerateOtpAuthUri(string email, string key)
    {
        const string issuer = "MyProject";
        return $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(email)}?secret={key}&issuer={Uri.EscapeDataString(issuer)}&digits=6";
    }
}
