using Microsoft.Extensions.Options;
using MyProject.Application.Cookies;
using MyProject.Application.Cookies.Constants;
using MyProject.Application.Features.Authentication.Dtos;
using MyProject.Infrastructure.Cryptography;
using MyProject.Infrastructure.Features.Authentication.Models;
using MyProject.Infrastructure.Features.Authentication.Options;
using MyProject.Infrastructure.Persistence;

namespace MyProject.Infrastructure.Features.Authentication.Services;

/// <summary>
/// Generates access and refresh token pairs, persists the refresh token,
/// and optionally writes authentication cookies.
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
}

/// <inheritdoc />
internal class TokenSessionService(
    ITokenProvider tokenProvider,
    TimeProvider timeProvider,
    ICookieService cookieService,
    IOptions<AuthenticationOptions> authenticationOptions,
    MyProjectDbContext dbContext) : ITokenSessionService
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
            expires: persistent ? utcNow.Add(_jwtOptions.AccessTokenLifetime) : null);

        cookieService.SetSecureCookie(
            key: CookieNames.RefreshToken,
            value: refreshToken,
            expires: persistent ? refreshTokenExpiry : null);
    }
}
