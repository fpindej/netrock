using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyProject.Application.Cookies;
using MyProject.Application.Cookies.Constants;
using MyProject.Application.Features.Audit;
using MyProject.Application.Features.Authentication;
using MyProject.Application.Features.Authentication.Dtos;
using MyProject.Application.Identity;
using MyProject.Application.Identity.Constants;
using MyProject.Infrastructure.Cryptography;
using MyProject.Infrastructure.Features.Authentication.Models;
using MyProject.Infrastructure.Features.Authentication.Options;
using MyProject.Infrastructure.Features.Authentication.Services.ExternalProviders;
using MyProject.Infrastructure.Persistence;
using MyProject.Shared;

namespace MyProject.Infrastructure.Features.Authentication.Services;

/// <summary>
/// Implementation of <see cref="IExternalAuthService"/> handling OAuth2 authorization flows,
/// account linking, and provider management.
/// </summary>
internal class ExternalAuthService(
    UserManager<ApplicationUser> userManager,
    ITokenProvider tokenProvider,
    TimeProvider timeProvider,
    ICookieService cookieService,
    IUserContext userContext,
    IAuditService auditService,
    IOptions<AuthenticationOptions> authenticationOptions,
    IOptions<ExternalAuthOptions> externalAuthOptions,
    IEnumerable<IExternalAuthProvider> providers,
    ILogger<ExternalAuthService> logger,
    MyProjectDbContext dbContext) : IExternalAuthService
{
    private readonly AuthenticationOptions.JwtOptions _jwtOptions = authenticationOptions.Value.Jwt;
    private readonly ExternalAuthOptions _externalOptions = externalAuthOptions.Value;

    /// <inheritdoc />
    public async Task<Result<ExternalChallengeOutput>> CreateChallengeAsync(
        ExternalChallengeInput input, CancellationToken cancellationToken)
    {
        if (!_externalOptions.AllowedRedirectUris.Contains(input.RedirectUri, StringComparer.OrdinalIgnoreCase))
        {
            return Result<ExternalChallengeOutput>.Failure(
                ErrorMessages.ExternalAuth.InvalidRedirectUri, ErrorType.Validation);
        }

        var provider = providers.FirstOrDefault(p =>
            string.Equals(p.Name, input.Provider, StringComparison.OrdinalIgnoreCase));

        if (provider is null)
        {
            return Result<ExternalChallengeOutput>.Failure(
                ErrorMessages.ExternalAuth.ProviderNotConfigured, ErrorType.Validation);
        }

        var stateToken = GenerateStateToken();
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var stateEntity = new ExternalAuthState
        {
            Id = Guid.NewGuid(),
            Token = HashHelper.Sha256(stateToken),
            Provider = provider.Name,
            RedirectUri = input.RedirectUri,
            UserId = userContext.UserId,
            CreatedAt = utcNow,
            ExpiresAt = utcNow.Add(_externalOptions.StateLifetime),
            IsUsed = false
        };

        dbContext.ExternalAuthStates.Add(stateEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        var authorizationUrl = provider.BuildAuthorizationUrl(stateToken, input.RedirectUri);

        return Result<ExternalChallengeOutput>.Success(new ExternalChallengeOutput(authorizationUrl));
    }

    /// <inheritdoc />
    public async Task<Result<ExternalCallbackOutput>> HandleCallbackAsync(
        ExternalCallbackInput input, bool useCookies, CancellationToken cancellationToken)
    {
        // 1. Validate state token
        var stateResult = await ResolveStateAsync(input.State, cancellationToken);
        if (!stateResult.IsSuccess)
        {
            return Result<ExternalCallbackOutput>.Failure(
                stateResult.Error ?? ErrorMessages.ExternalAuth.InvalidState,
                stateResult.ErrorType ?? ErrorType.Validation);
        }

        var state = stateResult.Value;

        var provider = providers.FirstOrDefault(p =>
            string.Equals(p.Name, state.Provider, StringComparison.OrdinalIgnoreCase));

        if (provider is null)
        {
            return Result<ExternalCallbackOutput>.Failure(
                ErrorMessages.ExternalAuth.ProviderNotConfigured, ErrorType.Validation);
        }

        // 2. Exchange code for user info
        ExternalUserInfo externalUser;
        try
        {
            externalUser = await provider.ExchangeCodeAsync(input.Code, state.RedirectUri, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Code exchange failed for provider {Provider}", state.Provider);
            return Result<ExternalCallbackOutput>.Failure(
                ErrorMessages.ExternalAuth.CodeExchangeFailed, ErrorType.Validation);
        }

        // 3. Check for existing link
        var existingLogin = await FindExistingLoginAsync(state.Provider, externalUser.ProviderKey, cancellationToken);
        var authenticatedUserId = state.UserId ?? userContext.UserId;

        if (existingLogin is not null)
        {
            return await HandleExistingLinkAsync(
                existingLogin, authenticatedUserId, state.Provider, useCookies, cancellationToken);
        }

        // 4. No existing link
        if (authenticatedUserId.HasValue)
        {
            return await LinkToAuthenticatedUserAsync(
                authenticatedUserId.Value, state.Provider, externalUser, useCookies, cancellationToken);
        }

        return await HandleUnauthenticatedAsync(
            state.Provider, externalUser, useCookies, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result> UnlinkProviderAsync(string provider, CancellationToken cancellationToken)
    {
        var userId = userContext.UserId;
        if (!userId.HasValue)
        {
            return Result.Failure(ErrorMessages.Auth.NotAuthenticated, ErrorType.Unauthorized);
        }

        var user = await userManager.FindByIdAsync(userId.Value.ToString());
        if (user is null)
        {
            return Result.Failure(ErrorMessages.Auth.UserNotFound, ErrorType.NotFound);
        }

        var logins = await userManager.GetLoginsAsync(user);
        var login = logins.FirstOrDefault(l =>
            string.Equals(l.LoginProvider, provider, StringComparison.OrdinalIgnoreCase));

        if (login is null)
        {
            return Result.Failure(ErrorMessages.ExternalAuth.ProviderNotLinked, ErrorType.Validation);
        }

        var hasPassword = await userManager.HasPasswordAsync(user);
        if (!hasPassword && logins.Count <= 1)
        {
            return Result.Failure(ErrorMessages.ExternalAuth.CannotUnlinkLastMethod, ErrorType.Validation);
        }

        var result = await userManager.RemoveLoginAsync(user, login.LoginProvider, login.ProviderKey);
        if (!result.Succeeded)
        {
            return Result.Failure(ErrorMessages.ExternalAuth.ProviderError);
        }

        await auditService.LogAsync(AuditActions.ExternalAccountUnlinked, userId: userId.Value,
            metadata: JsonSerializer.Serialize(new { provider }), ct: cancellationToken);

        return Result.Success();
    }

    /// <inheritdoc />
    public IReadOnlyList<ExternalProviderInfo> GetAvailableProviders() =>
        providers.Select(p => new ExternalProviderInfo(p.Name, p.DisplayName)).ToList();

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetLinkedProvidersAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return [];
        }

        var logins = await userManager.GetLoginsAsync(user);
        return logins.Select(l => l.LoginProvider).ToList();
    }

    /// <inheritdoc />
    public async Task<Result> SetPasswordAsync(SetPasswordInput input, CancellationToken cancellationToken)
    {
        var userId = userContext.UserId;
        if (!userId.HasValue)
        {
            return Result.Failure(ErrorMessages.Auth.NotAuthenticated, ErrorType.Unauthorized);
        }

        var user = await userManager.FindByIdAsync(userId.Value.ToString());
        if (user is null)
        {
            return Result.Failure(ErrorMessages.Auth.UserNotFound, ErrorType.NotFound);
        }

        if (await userManager.HasPasswordAsync(user))
        {
            return Result.Failure(ErrorMessages.ExternalAuth.PasswordAlreadySet, ErrorType.Validation);
        }

        var result = await userManager.AddPasswordAsync(user, input.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure(errors);
        }

        await auditService.LogAsync(AuditActions.PasswordSet, userId: userId.Value, ct: cancellationToken);

        return Result.Success();
    }

    private async Task<Result<ExternalAuthState>> ResolveStateAsync(
        string plainToken, CancellationToken cancellationToken)
    {
        var hashedToken = HashHelper.Sha256(plainToken);
        var state = await dbContext.ExternalAuthStates
            .FirstOrDefaultAsync(s => s.Token == hashedToken, cancellationToken);

        if (state is null || state.IsUsed)
        {
            return Result<ExternalAuthState>.Failure(
                ErrorMessages.ExternalAuth.InvalidState, ErrorType.Validation);
        }

        if (state.ExpiresAt < timeProvider.GetUtcNow().UtcDateTime)
        {
            return Result<ExternalAuthState>.Failure(
                ErrorMessages.ExternalAuth.StateExpired, ErrorType.Validation);
        }

        // Mark as used (single-use)
        state.IsUsed = true;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<ExternalAuthState>.Success(state);
    }

    private async Task<ApplicationUser?> FindExistingLoginAsync(
        string provider, string providerKey, CancellationToken cancellationToken)
    {
        var login = await dbContext.Set<IdentityUserLogin<Guid>>()
            .FirstOrDefaultAsync(l => l.LoginProvider == provider && l.ProviderKey == providerKey,
                cancellationToken);

        if (login is null)
        {
            return null;
        }

        return await userManager.FindByIdAsync(login.UserId.ToString());
    }

    private async Task<Result<ExternalCallbackOutput>> HandleExistingLinkAsync(
        ApplicationUser linkedUser, Guid? authenticatedUserId, string provider,
        bool useCookies, CancellationToken cancellationToken)
    {
        if (!authenticatedUserId.HasValue)
        {
            // Not logged in, existing link -> login
            var tokens = await GenerateTokensAsync(linkedUser, useCookies, cancellationToken);
            await auditService.LogAsync(AuditActions.ExternalLoginSuccess, userId: linkedUser.Id,
                metadata: JsonSerializer.Serialize(new { provider }), ct: cancellationToken);

            return Result<ExternalCallbackOutput>.Success(
                new ExternalCallbackOutput(tokens, IsNewUser: false, Provider: provider, IsLinkOnly: false));
        }

        if (authenticatedUserId.Value == linkedUser.Id)
        {
            // Already linked to this user
            return Result<ExternalCallbackOutput>.Success(
                new ExternalCallbackOutput(Tokens: null, IsNewUser: false, Provider: provider, IsLinkOnly: true));
        }

        // Linked to a different user
        return Result<ExternalCallbackOutput>.Failure(
            ErrorMessages.ExternalAuth.AlreadyLinkedToOtherUser, ErrorType.Validation);
    }

    private async Task<Result<ExternalCallbackOutput>> LinkToAuthenticatedUserAsync(
        Guid userId, string provider, ExternalUserInfo externalUser,
        bool useCookies, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result<ExternalCallbackOutput>.Failure(
                ErrorMessages.Auth.UserNotFound, ErrorType.NotFound);
        }

        var addResult = await userManager.AddLoginAsync(user,
            new UserLoginInfo(provider, externalUser.ProviderKey, provider));

        if (!addResult.Succeeded)
        {
            return Result<ExternalCallbackOutput>.Failure(
                ErrorMessages.ExternalAuth.ProviderError, ErrorType.Validation);
        }

        await auditService.LogAsync(AuditActions.ExternalAccountLinked, userId: userId,
            metadata: JsonSerializer.Serialize(new { provider }), ct: cancellationToken);

        return Result<ExternalCallbackOutput>.Success(
            new ExternalCallbackOutput(Tokens: null, IsNewUser: false, Provider: provider, IsLinkOnly: true));
    }

    private async Task<Result<ExternalCallbackOutput>> HandleUnauthenticatedAsync(
        string provider, ExternalUserInfo externalUser,
        bool useCookies, CancellationToken cancellationToken)
    {
        var existingUser = await userManager.FindByEmailAsync(externalUser.Email);

        if (existingUser is not null)
        {
            if (!existingUser.EmailConfirmed)
            {
                await auditService.LogAsync(AuditActions.ExternalLoginFailure,
                    metadata: JsonSerializer.Serialize(new { provider, email = externalUser.Email, reason = "email_not_verified" }),
                    ct: cancellationToken);

                return Result<ExternalCallbackOutput>.Failure(
                    ErrorMessages.ExternalAuth.EmailNotVerified, ErrorType.Validation);
            }

            // Auto-link by verified email
            var addResult = await userManager.AddLoginAsync(existingUser,
                new UserLoginInfo(provider, externalUser.ProviderKey, provider));

            if (!addResult.Succeeded)
            {
                return Result<ExternalCallbackOutput>.Failure(
                    ErrorMessages.ExternalAuth.ProviderError, ErrorType.Validation);
            }

            var tokens = await GenerateTokensAsync(existingUser, useCookies, cancellationToken);

            await auditService.LogAsync(AuditActions.ExternalAccountLinked, userId: existingUser.Id,
                metadata: JsonSerializer.Serialize(new { provider, autoLinked = true }), ct: cancellationToken);
            await auditService.LogAsync(AuditActions.ExternalLoginSuccess, userId: existingUser.Id,
                metadata: JsonSerializer.Serialize(new { provider }), ct: cancellationToken);

            return Result<ExternalCallbackOutput>.Success(
                new ExternalCallbackOutput(tokens, IsNewUser: false, Provider: provider, IsLinkOnly: false));
        }

        // Create new user
        return await CreateUserFromExternalAsync(provider, externalUser, useCookies, cancellationToken);
    }

    private async Task<Result<ExternalCallbackOutput>> CreateUserFromExternalAsync(
        string provider, ExternalUserInfo externalUser,
        bool useCookies, CancellationToken cancellationToken)
    {
        var user = new ApplicationUser
        {
            UserName = externalUser.Email,
            Email = externalUser.Email,
            EmailConfirmed = externalUser.EmailVerified,
            FirstName = externalUser.FirstName,
            LastName = externalUser.LastName
        };

        var createResult = await userManager.CreateAsync(user);
        if (!createResult.Succeeded)
        {
            var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
            logger.LogError("Failed to create user from external provider {Provider}: {Errors}", provider, errors);
            return Result<ExternalCallbackOutput>.Failure(errors);
        }

        var roleResult = await userManager.AddToRoleAsync(user, AppRoles.User);
        if (!roleResult.Succeeded)
        {
            logger.LogError("Failed to assign User role to externally created user {UserId}", user.Id);
        }

        var loginResult = await userManager.AddLoginAsync(user,
            new UserLoginInfo(provider, externalUser.ProviderKey, provider));

        if (!loginResult.Succeeded)
        {
            logger.LogError("Failed to link provider {Provider} to newly created user {UserId}",
                provider, user.Id);
        }

        var tokens = await GenerateTokensAsync(user, useCookies, cancellationToken);

        await auditService.LogAsync(AuditActions.ExternalAccountCreated, userId: user.Id,
            metadata: JsonSerializer.Serialize(new { provider }), ct: cancellationToken);

        return Result<ExternalCallbackOutput>.Success(
            new ExternalCallbackOutput(tokens, IsNewUser: true, Provider: provider, IsLinkOnly: false));
    }

    private async Task<AuthenticationOutput> GenerateTokensAsync(
        ApplicationUser user, bool useCookies, CancellationToken cancellationToken)
    {
        var accessToken = await tokenProvider.GenerateAccessToken(user);
        var refreshTokenString = tokenProvider.GenerateRefreshToken();
        var utcNow = timeProvider.GetUtcNow();

        var refreshLifetime = _jwtOptions.RefreshToken.SessionLifetime;

        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = HashHelper.Sha256(refreshTokenString),
            UserId = user.Id,
            CreatedAt = utcNow.UtcDateTime,
            ExpiredAt = utcNow.UtcDateTime.Add(refreshLifetime),
            IsUsed = false,
            IsInvalidated = false,
            IsPersistent = false
        };

        dbContext.RefreshTokens.Add(refreshTokenEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (useCookies)
        {
            cookieService.SetSecureCookie(
                key: CookieNames.AccessToken,
                value: accessToken,
                expires: null);

            cookieService.SetSecureCookie(
                key: CookieNames.RefreshToken,
                value: refreshTokenString,
                expires: null);
        }

        return new AuthenticationOutput(AccessToken: accessToken, RefreshToken: refreshTokenString);
    }

    private static string GenerateStateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }
}
