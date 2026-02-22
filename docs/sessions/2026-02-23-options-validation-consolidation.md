# Options Validation Consolidation

**Date**: 2026-02-23
**Scope**: Align all options classes with the gold-standard validation pattern established by `AuthenticationOptions` and `RateLimitingOptions`

## Summary

Consolidated options validation across `CaptchaOptions`, `CachingOptions`, and `JobSchedulingOptions` to match the established gold-standard pattern: `public sealed` classes, `[ValidateObjectMembers]` on nested properties for declarative recursive validation, self-guarding nested `Validate()` methods, and data annotations for simple constraints. This branch also includes prior work adding a resilient `CacheService` with an optional `Caching.Enabled` master switch, removing the hard Redis dependency from Docker, and related test coverage.

## Changes Made

| File | Change | Reason |
|------|--------|--------|
| `Infrastructure/Caching/Options/CachingOptions.cs` | Added `[ValidateObjectMembers]` on `Redis`/`InMemory` props, simplified root `Validate()` to only check `DefaultExpiration`, added `if (!Enabled) yield break;` self-guard to `RedisOptions.Validate()`, fixed misleading error message in `InMemoryOptions` | Align with declarative nested validation pattern; each nested class owns its own guards |
| `Infrastructure/Features/Captcha/Options/CaptchaOptions.cs` | `internal sealed` to `public sealed` | All other options classes are public; architecture tests only enforce `internal` on `*Service` classes |
| `Infrastructure/Features/Jobs/Options/JobSchedulingOptions.cs` | Added `[Range(1, 1000)]` on `WorkerCount` | Missing validation on a numeric property that should have bounds |
| `Infrastructure/Caching/Services/CacheService.cs` | Resilient cache service with try/catch around Redis operations | Prevent Redis failures from crashing the application |
| `Infrastructure/Caching/Services/NoOpCacheService.cs` | New no-op implementation for when caching is disabled | Support `Caching.Enabled = false` master switch |
| `Infrastructure/Caching/Extensions/ServiceCollectionExtensions.cs` | Register `NoOpCacheService` when caching disabled | Wire up the master switch |
| `deploy/docker-compose.yml` | Removed `depends_on: redis` from API service | Redis is now optional |
| `deploy/envs/*.env` | Added `Caching__Enabled` variable | Document and configure the master switch |
| `appsettings*.json` | Added `Caching.Enabled` key | Support the master switch across environments |
| `tests/**/CacheServiceTests.cs` | 237-line test suite for `CacheService` | Cover resilient behavior, expiration, error handling |
| `tests/**/NoOpCacheServiceTests.cs` | 74-line test suite for `NoOpCacheService` | Cover all interface methods return expected defaults |

## Decisions & Reasoning

### Declarative `[ValidateObjectMembers]` over manual foreach delegation

- **Choice**: Use `[ValidateObjectMembers]` attribute and let the framework recurse into nested options
- **Alternatives considered**: Keep manual `foreach` delegation in root `Validate()`
- **Reasoning**: The gold standard (`AuthenticationOptions`, `RateLimitingOptions`) already uses this pattern. It's declarative, less error-prone, and `.ValidateDataAnnotations()` in .NET 9+ natively handles the recursion without source generators

### `InMemoryOptions` always validates (no self-guard)

- **Choice**: Let `InMemoryOptions.Validate()` run unconditionally, even when Redis is the active provider
- **Alternatives considered**: Add an `Enabled` flag or parent-aware guard to skip validation
- **Reasoning**: The defaults are always valid, and catching bad config early is correct regardless of which provider is active. A future misconfiguration would surface immediately at startup rather than silently waiting until a Redis-to-InMemory failover

### `RedisOptions` self-guards on `Enabled`

- **Choice**: `RedisOptions.Validate()` starts with `if (!Enabled) yield break;`
- **Alternatives considered**: Leave guard in parent; use data annotations only
- **Reasoning**: `ConnectionString` is only required when Redis is enabled. Cross-property logic (`Enabled` gates `ConnectionString` requirement) requires `IValidatableObject` with a self-guard, matching the pattern where each nested class is responsible for its own preconditions

## Follow-Up Items

- [ ] Consider adding an `Enabled` flag to `InMemoryOptions` if use cases emerge where InMemory config should be intentionally invalid while Redis is active
