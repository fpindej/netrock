# JWT and Token Security Hardening

**Date**: 2026-02-22
**Scope**: Three minor JWT security hardening items from issue #145

## Summary

Added the `nbf` (not-before) claim to JWT tokens, wrapped cache eviction calls during security stamp rotation in try/catch to prevent transient Redis failures from disrupting Identity updates, and added startup validation that the configurable `SecurityStampClaimType` does not collide with registered JWT claim names. A code review pass fixed a dual-timestamp bug, extended cache resilience to `InvalidateUserCacheAsync`, and improved test coverage.

## Changes Made

| File | Change | Reason |
|------|--------|--------|
| `JwtTokenProvider.cs` | Added `notBefore: now` to `JwtSecurityToken`; derive both `now` and `expires` from a single `GetUtcNow()` call | Pins `nbf` to issuance time; avoids clock skew between `nbf` and `exp` |
| `RoleManagementService.cs` | Wrapped each `RemoveAsync` in its own try/catch; added informational log for affected user count | Independent evictions don't skip each other on failure; observability for bulk operations |
| `AdminService.cs` | Wrapped `RemoveAsync` in `RotateSecurityStampAsync`, `RevokeUserSessionsAsync`, and `InvalidateUserCacheAsync` with try/catch | Consistent Redis resilience across all cache eviction paths |
| `AuthenticationOptions.cs` | Added `[Required]` to `SecurityStampClaimType`; reserved claim collision check applies unconditionally (no `IsNullOrEmpty` guard) | Belt-and-suspenders: `[Required]` catches empty, collision check catches reserved names regardless |
| `AuthenticationOptionsValidationTests.cs` | 18 test cases: valid values, all reserved names, case-insensitive variants (`SUB`, `Email`), `ClaimTypes.Role` long-form URI, empty string | Full coverage of validation logic including edge cases |

## Decisions & Reasoning

### Cache eviction: log-only catch vs. rethrow

- **Choice**: Catch and log a warning, do not rethrow
- **Alternatives considered**: Rethrow (fails the entire operation), circuit breaker pattern
- **Reasoning**: The security stamp rotation (Identity DB update) is the critical operation. Cache eviction is best-effort — the entry will expire naturally or be overwritten on next access. Failing the admin action because of a transient cache issue is worse than a stale cache entry.

### Separate try/catch per cache key (RoleManagementService)

- **Choice**: Each `RemoveAsync` gets its own try/catch block
- **Alternatives considered**: Single try/catch wrapping both calls
- **Reasoning**: If the security stamp eviction fails transiently, the user cache eviction should still be attempted. They are independent operations on different keys.

### Reserved claim type validation: case-insensitive

- **Choice**: `StringComparer.OrdinalIgnoreCase` for the reserved set
- **Alternatives considered**: Case-sensitive matching
- **Reasoning**: JWT claim names are case-sensitive per RFC 7519, but a case-only difference (e.g. `Sub` vs `sub`) would be confusing and error-prone. Rejecting case variants is the safer default for a configuration guard.

### Unconditional collision check (no IsNullOrEmpty guard)

- **Choice**: `ReservedClaimTypes.Contains(SecurityStampClaimType)` without a null/empty pre-check
- **Alternatives considered**: Guard with `!string.IsNullOrEmpty` to skip check on empty values
- **Reasoning**: The `[Required]` attribute catches empty/null, but if validation orchestration ever changes, the collision check would silently pass for empty values. For a security-critical config property, defense-in-depth is preferred.

## Follow-Up Items

- [ ] #247 — `ValidateSecurityStampAsync` in the JWT auth pipeline also lacks Redis failure handling, causing 500s on all authenticated requests when Redis is down (separate, higher-impact issue)
