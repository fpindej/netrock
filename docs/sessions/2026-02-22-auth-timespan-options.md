# Refactor Auth Options to TimeSpan with Dual Refresh Token Lifetimes

**Date**: 2026-02-22
**Scope**: Backend authentication configuration — replace fixed-unit integer lifetimes with `TimeSpan`, add separate refresh token lifetimes for persistent vs session logins, extract `TimeSpan.ToHumanReadable()` to Shared

## Summary

Authentication token lifetimes were configured with fixed-unit integers (`ExpiresInMinutes`, `ExpiresInDays`, `ExpiresInHours`), which was inflexible. Additionally, both "remember me" and non-remember-me sessions shared the same 7-day refresh token expiry — meaning a browser left open without "remember me" stayed authenticated for up to 7 days.

Replaced all lifetime properties with `TimeSpan` for granular, consistent configuration. Added dual refresh token lifetimes (`PersistentLifetime` for remember-me, `SessionLifetime` for session logins) so non-persistent sessions expire after 24 hours instead of 7 days. Synced `DataProtectionTokenProviderOptions.TokenLifespan` from config instead of hardcoding 24h. Extracted `TimeSpan.ToHumanReadable()` as a general-purpose extension in Shared for reuse across the stack.

## Changes Made

| File | Change | Reason |
|------|--------|--------|
| `Infrastructure/.../Options/AuthenticationOptions.cs` | `ExpiresInMinutes` → `AccessTokenLifetime` (TimeSpan), `ExpiresInDays` → `PersistentLifetime` (TimeSpan), added `SessionLifetime` (TimeSpan), `ExpiresInHours` → `Lifetime` (TimeSpan). Added `IValidatableObject` on `JwtOptions`, `RefreshTokenOptions`, `EmailTokenOptions` | Flexible TimeSpan config with bounds validation |
| `Infrastructure/.../Extensions/ServiceCollectionExtensions.cs` | Sync `DataProtectionTokenProviderOptions.TokenLifespan` from config; replace `null!` with descriptive `InvalidOperationException` | Config-driven token lifespan, null safety |
| `Infrastructure/.../Services/JwtTokenProvider.cs` | `.AddMinutes(ExpiresInMinutes)` → `.Add(AccessTokenLifetime)` | Use new TimeSpan property |
| `Infrastructure/.../Services/AuthenticationService.cs` | Select `PersistentLifetime` vs `SessionLifetime` based on `rememberMe`; use `TimeSpan.ToHumanReadable()` for email body | Dual lifetime + human-readable display |
| `Infrastructure/.../Services/EmailTokenService.cs` | `.AddHours(ExpiresInHours)` → `.Add(Lifetime)` | Use new TimeSpan property |
| `Shared/TimeSpanExtensions.cs` | New `TimeSpan.ToHumanReadable()` extension (days → hours → minutes) | Reusable formatting across the stack |
| `appsettings.json` | TimeSpan format for all lifetimes, added `EmailToken` section with `Lifetime` | Explicit config for all lifetimes |
| `appsettings.Development.json` | `ExpiresInMinutes: 100` → `AccessTokenLifetime: "01:40:00"` | TimeSpan format |
| `.env.example` | Updated all env var names and examples | Match new property names |
| `SKILLS.md`, `docs/development.md` | Updated example env var references | Keep docs accurate |
| `tests/.../AuthenticationServiceTests.cs` | Updated options construction; added dual-lifetime tests; renamed redundant expiration test | Verify dual lifetime behavior |
| `tests/.../AuthenticationOptionsValidationTests.cs` | New — 24 tests for `IValidatableObject` bounds on all options classes | Boundary + cross-field validation coverage |
| `tests/.../TimeSpanExtensionsTests.cs` | New — 16 tests for `ToHumanReadable()` (days/hours/minutes/singular/plural/edge cases) | Full branch coverage for the extension |

## New Options Structure

```
AuthenticationOptions
├── Jwt (JwtOptions) : IValidatableObject
│   ├── Key: string
│   ├── Issuer: string
│   ├── Audience: string
│   ├── AccessTokenLifetime: TimeSpan       [default: 10 min, range: 1 min – 2 hours]
│   ├── SecurityStampClaimType: string
│   └── RefreshToken (RefreshTokenOptions) : IValidatableObject
│       ├── PersistentLifetime: TimeSpan     [default: 7 days, range: 1 day – 365 days]
│       └── SessionLifetime: TimeSpan        [default: 24 hours, range: 10 min – 30 days, ≤ PersistentLifetime]
└── EmailToken (EmailTokenOptions) : IValidatableObject
    ├── TokenLengthInBytes: int              [range: 16 – 128]
    └── Lifetime: TimeSpan                   [default: 24 hours, range: 1 hour – 7 days]
```

## Design Decisions

- **`IValidatableObject` over `IValidateOptions<T>`**: Consistent with existing caching options pattern — validation lives directly on the options class, validated via `ValidateDataAnnotations()`.
- **`RefreshTokenAsync` unchanged**: Rotated tokens already inherit `ExpiredAt` from the parent token, so the dual lifetime is only set at login time.
- **`TimeSpan.ToHumanReadable()` in Shared**: Extracted from auth-specific `FormatLifetime` to a general-purpose public extension. Uses the largest whole unit (days → hours → minutes) with singular/plural handling. Lives in `MyProject.Shared` so it's reusable across all layers without `InternalsVisibleTo` workarounds.
- **Null safety on config reads**: Replaced `null!` with `?? throw new InvalidOperationException(...)` for clear startup failure messages when configuration sections are missing.
