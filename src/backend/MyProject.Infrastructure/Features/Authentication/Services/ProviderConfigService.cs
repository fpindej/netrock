using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using MyProject.Application.Caching.Constants;
using MyProject.Application.Cryptography;
using MyProject.Application.Features.Audit;
using MyProject.Application.Features.Authentication;
using MyProject.Application.Features.Authentication.Dtos;
using MyProject.Infrastructure.Features.Authentication.Models;
using MyProject.Infrastructure.Features.Authentication.Options;
using MyProject.Infrastructure.Features.Authentication.Services.ExternalProviders;
using MyProject.Infrastructure.Persistence;

namespace MyProject.Infrastructure.Features.Authentication.Services;

/// <summary>
/// Manages OAuth provider configuration with DB storage and appsettings fallback.
/// Uses <see cref="HybridCache"/> with 5-minute TTL per provider.
/// </summary>
internal sealed class ProviderConfigService(
    MyProjectDbContext dbContext,
    ISecretEncryptionService encryptionService,
    IOptions<ExternalAuthOptions> externalAuthOptions,
    IEnumerable<IExternalAuthProvider> providers,
    HybridCache hybridCache,
    IAuditService auditService,
    TimeProvider timeProvider) : IProviderConfigService
{
    private static readonly HybridCacheEntryOptions CacheOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(5)
    };

    private readonly ExternalAuthOptions _options = externalAuthOptions.Value;

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
                var clientId = encryptionService.Decrypt(dbConfig.EncryptedClientId);
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
                var appSettings = GetAppSettingsOptions(provider.Name);
                result.Add(new ProviderConfigOutput(
                    provider.Name,
                    provider.DisplayName,
                    appSettings?.Enabled ?? false,
                    appSettings?.ClientId,
                    HasClientSecret: !string.IsNullOrEmpty(appSettings?.ClientSecret),
                    Source: "appsettings",
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
    public async Task UpsertAsync(
        Guid callerUserId, UpsertProviderConfigInput input, CancellationToken cancellationToken)
    {
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var existing = await dbContext.Set<ExternalProviderConfig>()
            .FirstOrDefaultAsync(c => c.Provider == input.Provider, cancellationToken);

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
                Provider = input.Provider,
                IsEnabled = input.IsEnabled,
                EncryptedClientId = encryptionService.Encrypt(input.ClientId),
                EncryptedClientSecret = input.ClientSecret is not null
                    ? encryptionService.Encrypt(input.ClientSecret)
                    : string.Empty,
                CreatedAt = utcNow,
                UpdatedAt = utcNow,
                UpdatedBy = callerUserId
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await hybridCache.RemoveAsync(CacheKeys.ProviderConfig(input.Provider), cancellationToken);

        await auditService.LogAsync(
            AuditActions.AdminUpdateOAuthProvider,
            userId: callerUserId,
            metadata: JsonSerializer.Serialize(new { provider = input.Provider, enabled = input.IsEnabled }),
            ct: cancellationToken);
    }

    private async Task<ProviderCredentialsOutput?> LoadCredentialsAsync(
        string provider, CancellationToken cancellationToken)
    {
        var dbConfig = await dbContext.Set<ExternalProviderConfig>()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Provider == provider, cancellationToken);

        if (dbConfig is not null)
        {
            if (!dbConfig.IsEnabled)
            {
                return null;
            }

            return new ProviderCredentialsOutput(
                encryptionService.Decrypt(dbConfig.EncryptedClientId),
                encryptionService.Decrypt(dbConfig.EncryptedClientSecret));
        }

        // Fallback to appsettings
        var appSettings = GetAppSettingsOptions(provider);
        if (appSettings is null || !appSettings.Enabled)
        {
            return null;
        }

        return new ProviderCredentialsOutput(appSettings.ClientId, appSettings.ClientSecret);
    }

    private ExternalAuthOptions.ProviderOptions? GetAppSettingsOptions(string provider)
    {
        return string.Equals(provider, "Google", StringComparison.OrdinalIgnoreCase) ? _options.Google
            : string.Equals(provider, "GitHub", StringComparison.OrdinalIgnoreCase) ? _options.GitHub
            : null;
    }
}
