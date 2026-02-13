using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace MyProject.WebApi.Authorization;

/// <summary>
/// Dynamically creates authorization policies for <c>"Permission:{name}"</c> policy names.
/// Policies are cached in a <see cref="ConcurrentDictionary{TKey,TValue}"/> since they are immutable.
/// Falls back to the default provider for all other policies.
/// </summary>
internal class PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : IAuthorizationPolicyProvider
{
    private const string Prefix = "Permission:";
    private readonly DefaultAuthorizationPolicyProvider _fallback = new(options);
    private readonly ConcurrentDictionary<string, AuthorizationPolicy> _cache = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            var policy = _cache.GetOrAdd(policyName, static name =>
            {
                // Normalize to lowercase to prevent case-sensitivity mismatches between
                // the policy name and the claim value checked by PermissionAuthorizationHandler.
                var permission = name[Prefix.Length..].ToLowerInvariant();
                return new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddRequirements(new PermissionRequirement(permission))
                    .Build();
            });

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallback.GetPolicyAsync(policyName);
    }

    /// <inheritdoc />
    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    /// <inheritdoc />
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();
}
