# JWT and Token Security Hardening

**Date**: 2026-02-22
**Scope**: Three minor JWT security hardening items from issue #145

## Summary

Added the `nbf` (not-before) claim to JWT tokens, wrapped cache eviction calls during security stamp rotation in try/catch to prevent transient Redis failures from disrupting Identity updates, and added startup validation that the configurable `SecurityStampClaimType` does not collide with registered JWT claim names.

## Changes Made

| File | Change | Reason |
|------|--------|--------|
| `src/backend/MyProject.Infrastructure/Features/Authentication/Services/JwtTokenProvider.cs` | Added `notBefore: now` parameter to `JwtSecurityToken` constructor | Pins the `nbf` claim to issuance time, preventing token validity before creation |
| `src/backend/MyProject.Infrastructure/Features/Admin/Services/RoleManagementService.cs` | Wrapped `cacheService.RemoveAsync` calls in try/catch with warning log; added informational log for affected user count | Cache failures during role permission changes no longer break stamp rotation |
| `src/backend/MyProject.Infrastructure/Features/Admin/Services/AdminService.cs` | Wrapped `cacheService.RemoveAsync` in try/catch in `RotateSecurityStampAsync` and `RevokeUserSessionsAsync` | Same resilience pattern — stamp rotation succeeds even if cache eviction fails |
| `src/backend/MyProject.Infrastructure/Features/Authentication/Options/AuthenticationOptions.cs` | Added `[Required]` to `SecurityStampClaimType`; added `ReservedClaimTypes` set and validation in `Validate()` | Prevents misconfiguration where the stamp claim would shadow a registered JWT claim |
| `src/backend/tests/MyProject.Component.Tests/Validation/AuthenticationOptionsValidationTests.cs` | Added 15 test cases for `SecurityStampClaimType` validation | Covers valid values, all reserved names, and empty string rejection |

## Decisions & Reasoning

### Cache eviction: log-only catch vs. rethrow

- **Choice**: Catch and log a warning, do not rethrow
- **Alternatives considered**: Rethrow (fails the entire operation), circuit breaker pattern
- **Reasoning**: The security stamp rotation (Identity DB update) is the critical operation. Cache eviction is best-effort — the entry will expire naturally or be overwritten on next access. Failing the admin action because of a transient cache issue is worse than a stale cache entry.

### Reserved claim type validation: case-insensitive

- **Choice**: `StringComparer.OrdinalIgnoreCase` for the reserved set
- **Alternatives considered**: Case-sensitive matching
- **Reasoning**: JWT claim names are case-sensitive per RFC 7519, but a case-only difference (e.g. `Sub` vs `sub`) would be confusing and error-prone. Rejecting case variants is the safer default for a configuration guard.

## Follow-Up Items

- [ ] #247 — `ValidateSecurityStampAsync` in the JWT auth pipeline also lacks Redis failure handling, causing 500s on all authenticated requests when Redis is down (separate, higher-impact issue)
