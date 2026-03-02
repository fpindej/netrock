using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using MyProject.Application.Caching.Constants;
using MyProject.Application.Cryptography;
using MyProject.Application.Features.Audit;
using MyProject.Application.Features.Authentication;
using MyProject.Application.Features.Authentication.Dtos;
using MyProject.Infrastructure.Features.Authentication.Models;
using MyProject.Infrastructure.Features.Authentication.Services.ExternalProviders;
using MyProject.Infrastructure.Persistence;
using MyProject.Shared;

namespace MyProject.Infrastructure.Features.Authentication.Services;

/// <summary>
/// Manages OAuth provider configuration with DB storage as the single source of truth.
/// Uses <see cref="HybridCache"/> with 5-minute TTL per provider.
/// </summary>
internal sealed class ProviderConfigService(
    MyProjectDbContext dbContext,
    ISecretEncryptionService encryptionService,
    IEnumerable<IExternalAuthProvider> providers,
    HybridCache hybridCache,
    IAuditService auditService,
    TimeProvider timeProvider,
    ILogger<ProviderConfigService> logger) : IProviderConfigService
{
    private static readonly HybridCacheEntryOptions CacheOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(5)
    };

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProviderConfigOutput>> GetAllAsync(CancellationToken cancellationToken)
    {
        var dbConfigs = await dbContext.Set<ExternalProviderConfig>()
            .AsNoTracking()
            .ToDictionaryAsync(c => c.Provider, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var result = new List<ProviderConfigOutput>();

        foreach (var provider in providers)
        {
            if (dbConfigs.TryGetValue(provider.Name, out var dbConfig))
            {
                string? clientId;
                try
                {
                    clientId = encryptionService.Decrypt(dbConfig.EncryptedClientId);
                }
                catch (CryptographicException ex)
                {
                    logger.LogError(ex, "Failed to decrypt client ID for provider {Provider}", provider.Name);
                    clientId = null;
                }

                result.Add(new ProviderConfigOutput(
                    provider.Name,
                    provider.DisplayName,
                    dbConfig.IsEnabled,
                    clientId,
                    HasClientSecret: !string.IsNullOrEmpty(dbConfig.EncryptedClientSecret),
                    Source: "database",
                    dbConfig.UpdatedAt,
                    dbConfig.UpdatedBy));
            }
            else
            {
                result.Add(new ProviderConfigOutput(
                    provider.Name,
                    provider.DisplayName,
                    IsEnabled: false,
                    ClientId: null,
                    HasClientSecret: false,
                    Source: "unconfigured",
                    UpdatedAt: null,
                    UpdatedBy: null));
            }
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<ProviderCredentialsOutput?> GetCredentialsAsync(
        string provider, CancellationToken cancellationToken)
    {
        var cached = await hybridCache.GetOrCreateAsync(
            CacheKeys.ProviderConfig(provider),
            async ct => await LoadCredentialsAsync(provider, ct),
            CacheOptions,
            cancellationToken: cancellationToken);

        return cached;
    }

    /// <inheritdoc />
    public async Task<Result> UpsertAsync(
        Guid callerUserId, UpsertProviderConfigInput input, CancellationToken cancellationToken)
    {
        var knownProvider = providers.FirstOrDefault(
            p => string.Equals(p.Name, input.Provider, StringComparison.OrdinalIgnoreCase));

        if (knownProvider is null)
        {
            return Result.Failure(ErrorMessages.ExternalAuth.UnknownProvider, ErrorType.Validation);
        }

        var canonicalName = knownProvider.Name;
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var existing = await dbContext.Set<ExternalProviderConfig>()
            .FirstOrDefaultAsync(c => c.Provider == canonicalName, cancellationToken);

        if (existing is not null)
        {
            existing.IsEnabled = input.IsEnabled;
            existing.EncryptedClientId = encryptionService.Encrypt(input.ClientId);

            if (input.ClientSecret is not null)
            {
                existing.EncryptedClientSecret = encryptionService.Encrypt(input.ClientSecret);
            }

            existing.UpdatedAt = utcNow;
            existing.UpdatedBy = callerUserId;
        }
        else
        {
            dbContext.Set<ExternalProviderConfig>().Add(new ExternalProviderConfig
            {
                Id = Guid.NewGuid(),
                Provider = canonicalName,
                IsEnabled = input.IsEnabled,
                EncryptedClientId = encryptionService.Encrypt(input.ClientId),
                EncryptedClientSecret = input.ClientSecret is not null
                    ? encryptionService.Encrypt(input.ClientSecret)
                    : string.Empty,
                CreatedAt = utcNow
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await hybridCache.RemoveAsync(CacheKeys.ProviderConfig(canonicalName), cancellationToken);

        await auditService.LogAsync(
            AuditActions.AdminUpdateOAuthProvider,
            userId: callerUserId,
            metadata: JsonSerializer.Serialize(new { provider = canonicalName, enabled = input.IsEnabled }),
            ct: cancellationToken);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> TestConnectionAsync(
        Guid callerUserId, string provider, CancellationToken cancellationToken)
    {
        var knownProvider = providers.FirstOrDefault(
            p => string.Equals(p.Name, provider, StringComparison.OrdinalIgnoreCase));

        if (knownProvider is null)
        {
            return Result.Failure(ErrorMessages.ExternalAuth.UnknownProvider, ErrorType.Validation);
        }

        var canonicalName = knownProvider.Name;

        var dbConfig = await dbContext.Set<ExternalProviderConfig>()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Provider == canonicalName, cancellationToken);

        if (dbConfig is null || string.IsNullOrEmpty(dbConfig.EncryptedClientId)
                             || string.IsNullOrEmpty(dbConfig.EncryptedClientSecret))
        {
            return Result.Failure(ErrorMessages.ExternalAuth.TestConnectionNotConfigured, ErrorType.Validation);
        }

        var credentials = new ProviderCredentials(
            encryptionService.Decrypt(dbConfig.EncryptedClientId),
            encryptionService.Decrypt(dbConfig.EncryptedClientSecret));

        var result = await knownProvider.TestConnectionAsync(credentials, cancellationToken);

        await auditService.LogAsync(
            AuditActions.AdminTestOAuthProvider,
            userId: callerUserId,
            metadata: JsonSerializer.Serialize(new { provider = canonicalName, success = result.IsSuccess }),
            ct: cancellationToken);

        return result;
    }

    private async Task<ProviderCredentialsOutput?> LoadCredentialsAsync(
        string provider, CancellationToken cancellationToken)
    {
        var dbConfig = await dbContext.Set<ExternalProviderConfig>()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Provider == provider, cancellationToken);

        if (dbConfig is null || !dbConfig.IsEnabled)
        {
            return null;
        }

        var clientSecret = string.IsNullOrEmpty(dbConfig.EncryptedClientSecret)
            ? string.Empty
            : encryptionService.Decrypt(dbConfig.EncryptedClientSecret);

        return new ProviderCredentialsOutput(
            encryptionService.Decrypt(dbConfig.EncryptedClientId),
            clientSecret);
    }
}
