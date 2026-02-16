using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyProject.Shared;
using MyProject.Infrastructure.Cryptography;
using MyProject.Infrastructure.Features.Authentication.Models;
using MyProject.Infrastructure.Features.Authentication.Options;
using MyProject.Infrastructure.Features.Email.Options;
using MyProject.Infrastructure.Persistence;
using MyProject.Application.Cookies.Constants;
using MyProject.Application.Features.Authentication;
using MyProject.Application.Features.Authentication.Dtos;
using MyProject.Application.Features.Email;
using MyProject.Application.Cookies;
using MyProject.Application.Caching;
using MyProject.Application.Caching.Constants;
using MyProject.Application.Identity;
using MyProject.Application.Identity.Constants;

namespace MyProject.Infrastructure.Features.Authentication.Services;

/// <summary>
/// Identity-backed implementation of <see cref="IAuthenticationService"/> with JWT token rotation.
/// </summary>
internal class AuthenticationService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ITokenProvider tokenProvider,
    TimeProvider timeProvider,
    ICookieService cookieService,
    IUserContext userContext,
    ICacheService cacheService,
    IEmailService emailService,
    IOptions<AuthenticationOptions> authenticationOptions,
    IOptions<EmailOptions> emailOptions,
    ILogger<AuthenticationService> logger,
    MyProjectDbContext dbContext) : IAuthenticationService
{
    private readonly AuthenticationOptions.JwtOptions _jwtOptions = authenticationOptions.Value.Jwt;
    private readonly EmailOptions _emailOptions = emailOptions.Value;

    /// <inheritdoc />
    public async Task<Result<AuthenticationOutput>> Login(string username, string password, bool useCookies = false, bool rememberMe = false, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByNameAsync(username);

        if (user is null)
        {
            return Result<AuthenticationOutput>.Failure(ErrorMessages.Auth.LoginInvalidCredentials, ErrorType.Unauthorized);
        }

        var signInResult = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        if (signInResult.IsLockedOut)
        {
            return Result<AuthenticationOutput>.Failure(ErrorMessages.Auth.LoginAccountLocked, ErrorType.Unauthorized);
        }

        if (!signInResult.Succeeded)
        {
            return Result<AuthenticationOutput>.Failure(ErrorMessages.Auth.LoginInvalidCredentials, ErrorType.Unauthorized);
        }

        var accessToken = await tokenProvider.GenerateAccessToken(user);
        var refreshTokenString = tokenProvider.GenerateRefreshToken();
        var utcNow = timeProvider.GetUtcNow();

        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = HashHelper.Sha256(refreshTokenString),
            UserId = user.Id,
            CreatedAt = utcNow.UtcDateTime,
            ExpiredAt = utcNow.UtcDateTime.AddDays(_jwtOptions.RefreshToken.ExpiresInDays),
            IsUsed = false,
            IsInvalidated = false,
            IsPersistent = rememberMe
        };

        dbContext.RefreshTokens.Add(refreshTokenEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (useCookies)
        {
            SetAuthCookies(accessToken, refreshTokenString, rememberMe, utcNow,
                utcNow.AddDays(_jwtOptions.RefreshToken.ExpiresInDays));
        }

        var output = new AuthenticationOutput(
            AccessToken: accessToken,
            RefreshToken: refreshTokenString
        );

        return Result<AuthenticationOutput>.Success(output);
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> Register(RegisterInput input, CancellationToken cancellationToken = default)
    {
        var normalizedPhone = PhoneNumberHelper.Normalize(input.PhoneNumber);

        if (normalizedPhone is not null && await IsPhoneNumberTakenAsync(normalizedPhone, excludeUserId: null))
        {
            return Result<Guid>.Failure(ErrorMessages.User.PhoneNumberTaken);
        }

        var user = new ApplicationUser
        {
            UserName = input.Email,
            Email = input.Email,
            FirstName = input.FirstName,
            LastName = input.LastName,
            PhoneNumber = normalizedPhone
        };

        var result = await userManager.CreateAsync(user, input.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result<Guid>.Failure(errors);
        }

        var roleResult = await userManager.AddToRoleAsync(user, AppRoles.User);

        if (!roleResult.Succeeded)
        {
            return Result<Guid>.Failure(ErrorMessages.Auth.RegisterRoleAssignFailed);
        }

        await SendVerificationEmailAsync(user, cancellationToken);

        return Result<Guid>.Success(user.Id);
    }

    /// <inheritdoc />
    public async Task Logout(CancellationToken cancellationToken = default)
    {
        // Get user ID before clearing cookies
        var userId = userContext.UserId;

        cookieService.DeleteCookie(CookieNames.AccessToken);
        cookieService.DeleteCookie(CookieNames.RefreshToken);

        if (userId.HasValue)
        {
            await RevokeUserTokens(userId.Value);
        }
    }

    /// <inheritdoc />
    public async Task<Result<AuthenticationOutput>> RefreshTokenAsync(string refreshToken, bool useCookies = false, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            return Result<AuthenticationOutput>.Failure(ErrorMessages.Auth.TokenMissing, ErrorType.Unauthorized);
        }

        var hashedToken = HashHelper.Sha256(refreshToken);
        var storedToken = await dbContext.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == hashedToken, cancellationToken);

        if (storedToken is null)
        {
            return Fail(ErrorMessages.Auth.TokenNotFound);
        }

        if (storedToken.IsInvalidated)
        {
            return Fail(ErrorMessages.Auth.TokenInvalidated);
        }

        if (storedToken.IsUsed)
        {
            // Security alert: Token reuse! Revoke all tokens for this user.
            storedToken.IsInvalidated = true;
            await RevokeUserTokens(storedToken.UserId, cancellationToken);
            return Fail(ErrorMessages.Auth.TokenReused);
        }

        if (storedToken.ExpiredAt < timeProvider.GetUtcNow().UtcDateTime)
        {
            storedToken.IsInvalidated = true;
            await dbContext.SaveChangesAsync(cancellationToken);
            return Fail(ErrorMessages.Auth.TokenExpired);
        }

        // Mark current token as used
        storedToken.IsUsed = true;

        var user = storedToken.User;
        if (user is null)
        {
            return Result<AuthenticationOutput>.Failure(ErrorMessages.Auth.TokenUserNotFound, ErrorType.Unauthorized);
        }

        var newAccessToken = await tokenProvider.GenerateAccessToken(user);
        var newRefreshTokenString = tokenProvider.GenerateRefreshToken();
        var utcNow = timeProvider.GetUtcNow();

        var newRefreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = HashHelper.Sha256(newRefreshTokenString),
            UserId = user.Id,
            CreatedAt = utcNow.UtcDateTime,
            ExpiredAt = storedToken.ExpiredAt,
            IsUsed = false,
            IsInvalidated = false,
            IsPersistent = storedToken.IsPersistent
        };

        dbContext.RefreshTokens.Add(newRefreshTokenEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (useCookies)
        {
            SetAuthCookies(newAccessToken, newRefreshTokenString, storedToken.IsPersistent, utcNow,
                new DateTimeOffset(storedToken.ExpiredAt, TimeSpan.Zero));
        }

        var output = new AuthenticationOutput(
            AccessToken: newAccessToken,
            RefreshToken: newRefreshTokenString
        );

        return Result<AuthenticationOutput>.Success(output);

        Result<AuthenticationOutput> Fail(string message)
        {
            if (useCookies)
            {
                cookieService.DeleteCookie(CookieNames.AccessToken);
                cookieService.DeleteCookie(CookieNames.RefreshToken);
            }
            return Result<AuthenticationOutput>.Failure(message, ErrorType.Unauthorized);
        }
    }

    /// <inheritdoc />
    public async Task<Result> ChangePasswordAsync(ChangePasswordInput input, CancellationToken cancellationToken = default)
    {
        var userId = userContext.UserId;

        if (!userId.HasValue)
        {
            return Result.Failure(ErrorMessages.Auth.NotAuthenticated, ErrorType.Unauthorized);
        }

        var user = await userManager.FindByIdAsync(userId.Value.ToString());

        if (user is null)
        {
            return Result.Failure(ErrorMessages.Auth.UserNotFound);
        }

        var passwordValid = await userManager.CheckPasswordAsync(user, input.CurrentPassword);

        if (!passwordValid)
        {
            return Result.Failure(ErrorMessages.Auth.PasswordIncorrect);
        }

        var changeResult = await userManager.ChangePasswordAsync(user, input.CurrentPassword, input.NewPassword);

        if (!changeResult.Succeeded)
        {
            var errors = string.Join(", ", changeResult.Errors.Select(e => e.Description));
            return Result.Failure(errors);
        }

        await RevokeUserTokens(userId.Value, cancellationToken);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> ForgotPasswordAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            // Return success to prevent user enumeration
            logger.LogDebug("Forgot password requested for non-existent email {Email}", email);
            return Result.Success();
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = Uri.EscapeDataString(token);
        var encodedEmail = Uri.EscapeDataString(email);
        var resetUrl = $"{_emailOptions.FrontendBaseUrl.TrimEnd('/')}/reset-password?token={encodedToken}&email={encodedEmail}";

        var safeResetUrl = WebUtility.HtmlEncode(resetUrl);
        var htmlBody = $"""
            <h2>Reset Your Password</h2>
            <p>You requested a password reset. Click the link below to set a new password:</p>
            <p><a href="{safeResetUrl}">Reset Password</a></p>
            <p>If you didn't request this, you can safely ignore this email.</p>
            <p>This link will expire in 24 hours.</p>
            """;

        var plainTextBody = $"""
            Reset Your Password

            You requested a password reset. Visit the following link to set a new password:
            {resetUrl}

            If you didn't request this, you can safely ignore this email.
            """;

        var message = new EmailMessage(
            To: email,
            Subject: "Reset Your Password",
            HtmlBody: htmlBody,
            PlainTextBody: plainTextBody
        );

        await SendEmailSafeAsync(message, cancellationToken);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> ResetPasswordAsync(ResetPasswordInput input, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(input.Email);

        if (user is null)
        {
            return Result.Failure(ErrorMessages.Auth.ResetPasswordFailed);
        }

        var resetResult = await userManager.ResetPasswordAsync(user, input.Token, input.NewPassword);

        if (!resetResult.Succeeded)
        {
            var errors = resetResult.Errors.Select(e => e.Description).ToList();

            // Distinguish between invalid token and other Identity errors (e.g., password policy)
            if (errors.Any(e => e.Contains("Invalid token", StringComparison.OrdinalIgnoreCase)))
            {
                return Result.Failure(ErrorMessages.Auth.ResetPasswordTokenInvalid);
            }

            return Result.Failure(string.Join(" ", errors));
        }

        await RevokeUserTokens(user.Id, cancellationToken);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> VerifyEmailAsync(VerifyEmailInput input, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(input.Email);

        if (user is null)
        {
            return Result.Failure(ErrorMessages.Auth.EmailVerificationFailed);
        }

        if (user.EmailConfirmed)
        {
            return Result.Failure(ErrorMessages.Auth.EmailAlreadyVerified);
        }

        var confirmResult = await userManager.ConfirmEmailAsync(user, input.Token);

        if (!confirmResult.Succeeded)
        {
            return Result.Failure(ErrorMessages.Auth.EmailVerificationFailed);
        }

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> ResendVerificationEmailAsync(CancellationToken cancellationToken = default)
    {
        var userId = userContext.UserId;

        if (!userId.HasValue)
        {
            return Result.Failure(ErrorMessages.Auth.NotAuthenticated, ErrorType.Unauthorized);
        }

        var user = await userManager.FindByIdAsync(userId.Value.ToString());

        if (user is null)
        {
            return Result.Failure(ErrorMessages.Auth.UserNotFound);
        }

        if (user.EmailConfirmed)
        {
            return Result.Failure(ErrorMessages.Auth.EmailAlreadyVerified);
        }

        await SendVerificationEmailAsync(user, cancellationToken);

        return Result.Success();
    }

    /// <summary>
    /// Sets access and refresh token cookies. When <paramref name="persistent"/> is true,
    /// cookies receive explicit expiry dates so they survive browser restarts.
    /// When false, session cookies are used (no <c>Expires</c> header).
    /// </summary>
    private void SetAuthCookies(string accessToken, string refreshToken, bool persistent,
        DateTimeOffset utcNow, DateTimeOffset refreshTokenExpiry)
    {
        cookieService.SetSecureCookie(
            key: CookieNames.AccessToken,
            value: accessToken,
            expires: persistent ? utcNow.AddMinutes(_jwtOptions.ExpiresInMinutes) : null);

        cookieService.SetSecureCookie(
            key: CookieNames.RefreshToken,
            value: refreshToken,
            expires: persistent ? refreshTokenExpiry : null);
    }

    private async Task RevokeUserTokens(Guid userId, CancellationToken cancellationToken = default)
    {
        var tokens = await dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsInvalidated)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.IsInvalidated = true;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user != null)
        {
            await userManager.UpdateSecurityStampAsync(user);
            await cacheService.RemoveAsync(CacheKeys.SecurityStamp(userId), cancellationToken);
        }
    }

    /// <summary>
    /// Sends an email, swallowing delivery failures. Transient provider outages
    /// (quota, auth, network) are logged but never propagate to the caller.
    /// </summary>
    private async Task SendEmailSafeAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        try
        {
            await emailService.SendEmailAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {To}", message.To);
        }
    }

    /// <summary>
    /// Sends a verification email to the specified user with a confirmation link.
    /// </summary>
    private async Task SendVerificationEmailAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            logger.LogWarning("Cannot send verification email: user {UserId} has no email address", user.Id);
            return;
        }

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = Uri.EscapeDataString(token);
        var encodedEmail = Uri.EscapeDataString(user.Email);
        var verifyUrl = $"{_emailOptions.FrontendBaseUrl.TrimEnd('/')}/verify-email?token={encodedToken}&email={encodedEmail}";

        var safeVerifyUrl = WebUtility.HtmlEncode(verifyUrl);
        var htmlBody = $"""
            <h2>Verify Your Email Address</h2>
            <p>Thank you for registering. Please click the link below to verify your email address:</p>
            <p><a href="{safeVerifyUrl}">Verify Email</a></p>
            <p>If you didn't create an account, you can safely ignore this email.</p>
            """;

        var plainTextBody = $"""
            Verify Your Email Address

            Thank you for registering. Visit the following link to verify your email address:
            {verifyUrl}

            If you didn't create an account, you can safely ignore this email.
            """;

        var message = new EmailMessage(
            To: user.Email,
            Subject: "Verify Your Email Address",
            HtmlBody: htmlBody,
            PlainTextBody: plainTextBody
        );

        await SendEmailSafeAsync(message, cancellationToken);
    }

    /// <summary>
    /// Checks whether any existing user already has the given normalized phone number.
    /// </summary>
    private async Task<bool> IsPhoneNumberTakenAsync(string normalizedPhone, Guid? excludeUserId)
    {
        return await userManager.Users
            .AnyAsync(u =>
                u.PhoneNumber != null
                && u.PhoneNumber == normalizedPhone
                && (!excludeUserId.HasValue || u.Id != excludeUserId.Value));
    }
}
