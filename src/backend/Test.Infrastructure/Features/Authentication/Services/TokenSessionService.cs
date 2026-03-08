using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using Test.Application.Caching.Constants;
using Test.Application.Cookies;
using Test.Application.Cookies.Constants;
using Test.Application.Features.Authentication.Dtos;
using Test.Infrastructure.Cryptography;
using Test.Infrastructure.Features.Authentication.Models;
using Test.Infrastructure.Features.Authentication.Options;
using Test.Infrastructure.Persistence;
using Test.Shared;

namespace Test.Infrastructure.Features.Authentication.Services;

/// <summary>
/// Manages the full token-session lifecycle: creation, refresh/rotation,
/// revocation, and authentication cookie management.
/// </summary>
internal interface ITokenSessionService
{
    /// <summary>
    /// Generates access and refresh tokens for the specified user, stores the refresh token,
    /// and optionally sets authentication cookies.
    /// </summary>
    /// <param name="user">The user to generate tokens for.</param>
    /// <param name="useCookies">Whether to set authentication cookies on the response.</param>
    /// <param name="rememberMe">Whether to use persistent (remember-me) token lifetimes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated access and refresh tokens.</returns>
    Task<AuthenticationOutput> GenerateTokensAsync(
        ApplicationUser user, bool useCookies, bool rememberMe, CancellationToken cancellationToken);

    /// <summary>
    /// Rotates a refresh token: validates the existing token, marks it as used,
    /// generates a new access/refresh pair, and optionally sets cookies.
    /// Detects token reuse and revokes all user sessions when found.
    /// </summary>
    /// <param name="refreshToken">The plaintext refresh token to rotate.</param>
    /// <param name="useCookies">Whether to set authentication cookies on the response.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new access and refresh tokens, or an error result.</returns>
    Task<Result<AuthenticationOutput>> RefreshTokenAsync(
        string refreshToken, bool useCookies, CancellationToken cancellationToken);

    /// <summary>
    /// Revokes all active refresh tokens for a user, updates the security stamp
    /// to invalidate existing JWTs, and clears the security stamp cache.
    /// </summary>
    /// <param name="userId">The user whose sessions should be revoked.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RevokeUserTokensAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets access and refresh token cookies. When <paramref name="persistent"/> is true,
    /// cookies receive explicit expiry dates so they survive browser restarts.
    /// When false, session cookies are used (no <c>Expires</c> header).
    /// </summary>
    /// <param name="accessToken">The JWT access token value.</param>
    /// <param name="refreshToken">The refresh token value.</param>
    /// <param name="persistent">Whether to set persistent cookies with explicit expiry.</param>
    /// <param name="utcNow">The current UTC time for computing access token cookie expiry.</param>
    /// <param name="refreshTokenExpiry">The absolute expiry for the refresh token cookie.</param>
    void SetAuthCookies(string accessToken, string refreshToken, bool persistent,
        DateTimeOffset utcNow, DateTimeOffset refreshTokenExpiry);

    /// <summary>
    /// Deletes access and refresh token cookies from the response.
    /// </summary>
    void DeleteAuthCookies();
}

/// <inheritdoc />
internal class TokenSessionService(
    ITokenProvider tokenProvider,
    TimeProvider timeProvider,
    ICookieService cookieService,
    UserManager<ApplicationUser> userManager,
    HybridCache hybridCache,
    IOptions<AuthenticationOptions> authenticationOptions,
    TestDbContext dbContext) : ITokenSessionService
{
    private readonly AuthenticationOptions.JwtOptions _jwtOptions = authenticationOptions.Value.Jwt;

    /// <inheritdoc />
    public async Task<AuthenticationOutput> GenerateTokensAsync(
        ApplicationUser user, bool useCookies, bool rememberMe, CancellationToken cancellationToken)
    {
        var accessToken = await tokenProvider.GenerateAccessToken(user);
        var refreshTokenString = tokenProvider.GenerateRefreshToken();
        var utcNow = timeProvider.GetUtcNow();

        var refreshLifetime = rememberMe
            ? _jwtOptions.RefreshToken.PersistentLifetime
            : _jwtOptions.RefreshToken.SessionLifetime;

        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = HashHelper.Sha256(refreshTokenString),
            UserId = user.Id,
            CreatedAt = utcNow.UtcDateTime,
            ExpiredAt = utcNow.UtcDateTime.Add(refreshLifetime),
            IsUsed = false,
            IsInvalidated = false,
            IsPersistent = rememberMe
        };

        dbContext.RefreshTokens.Add(refreshTokenEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (useCookies)
        {
            SetAuthCookies(accessToken, refreshTokenString, rememberMe, utcNow,
                utcNow.Add(refreshLifetime));
        }

        return new AuthenticationOutput(AccessToken: accessToken, RefreshToken: refreshTokenString);
    }

    /// <inheritdoc />
    public async Task<Result<AuthenticationOutput>> RefreshTokenAsync(
        string refreshToken, bool useCookies, CancellationToken cancellationToken)
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
            await RevokeUserTokensAsync(storedToken.UserId, cancellationToken);
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

        return Result<AuthenticationOutput>.Success(
            new AuthenticationOutput(AccessToken: newAccessToken, RefreshToken: newRefreshTokenString));

        Result<AuthenticationOutput> Fail(string message)
        {
            if (useCookies)
            {
                DeleteAuthCookies();
            }
            return Result<AuthenticationOutput>.Failure(message, ErrorType.Unauthorized);
        }
    }

    /// <inheritdoc />
    public async Task RevokeUserTokensAsync(Guid userId, CancellationToken cancellationToken = default)
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
            await hybridCache.RemoveAsync(CacheKeys.SecurityStamp(userId), cancellationToken);
        }
    }

    /// <inheritdoc />
    public void SetAuthCookies(string accessToken, string refreshToken, bool persistent,
        DateTimeOffset utcNow, DateTimeOffset refreshTokenExpiry)
    {
        cookieService.SetSecureCookie(
            key: CookieNames.AccessToken,
            value: accessToken,
            expires: persistent ? utcNow.Add(_jwtOptions.AccessTokenLifetime) : null);

        cookieService.SetSecureCookie(
            key: CookieNames.RefreshToken,
            value: refreshToken,
            expires: persistent ? refreshTokenExpiry : null);
    }

    /// <inheritdoc />
    public void DeleteAuthCookies()
    {
        cookieService.DeleteCookie(CookieNames.AccessToken);
        cookieService.DeleteCookie(CookieNames.RefreshToken);
    }
}
