using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MyProject.Infrastructure.Cryptography;
using MyProject.Infrastructure.Features.Authentication.Models;
using MyProject.Infrastructure.Features.Authentication.Options;
using MyProject.Infrastructure.Persistence;

namespace MyProject.Infrastructure.Features.Authentication.Services;

/// <summary>
/// Manages opaque email tokens that map to ASP.NET Identity tokens in the database.
/// Used by both <see cref="AuthenticationService"/> and admin flows to avoid exposing
/// Identity tokens or user emails in URLs.
/// </summary>
internal class EmailTokenService(
    MyProjectDbContext dbContext,
    TimeProvider timeProvider,
    IOptions<AuthenticationOptions> authenticationOptions)
{
    private readonly AuthenticationOptions.EmailTokenOptions _emailTokenOptions = authenticationOptions.Value.EmailToken;

    /// <summary>
    /// Generates a cryptographically random opaque token, stores its SHA-256 hash alongside
    /// the ASP.NET Identity token in the database, and returns the raw token for inclusion in URLs.
    /// </summary>
    public async Task<string> CreateAsync(
        Guid userId, string identityToken, EmailTokenPurpose purpose, CancellationToken cancellationToken)
    {
        var rawToken = RandomNumberGenerator.GetHexString(_emailTokenOptions.TokenLengthInBytes * 2, lowercase: true);
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var entity = new EmailToken
        {
            Id = Guid.NewGuid(),
            Token = HashHelper.Sha256(rawToken),
            IdentityToken = identityToken,
            Purpose = purpose,
            CreatedAt = utcNow,
            ExpiresAt = utcNow.AddHours(_emailTokenOptions.ExpiresInHours),
            IsUsed = false,
            UserId = userId
        };

        dbContext.EmailTokens.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return rawToken;
    }

    /// <summary>
    /// Looks up an email token by its raw value. Returns <c>null</c> when the token
    /// is not found, already used, expired, or has a mismatched purpose.
    /// </summary>
    public async Task<EmailToken?> ResolveAsync(
        string rawToken, EmailTokenPurpose expectedPurpose, CancellationToken cancellationToken)
    {
        var hash = HashHelper.Sha256(rawToken);
        var token = await dbContext.EmailTokens
            .FirstOrDefaultAsync(t => t.Token == hash, cancellationToken);

        if (token is null || token.IsUsed || token.Purpose != expectedPurpose)
        {
            return null;
        }

        if (token.ExpiresAt < timeProvider.GetUtcNow().UtcDateTime)
        {
            return null;
        }

        return token;
    }
}
