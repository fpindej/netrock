# Documentation Accuracy Overhaul

**Date**: 2026-02-24
**Scope**: Fix factual inaccuracies, stale information, and gaps across all documentation files

## Summary

Comprehensive audit of every documentation file in the repository, cross-referenced against actual source code. Fixed 14 factual inaccuracies, 7 stale descriptions, and filled key gaps in FILEMAP.md. Every claim in the documentation now matches the codebase.

## Changes Made

| File | Change | Reason |
|------|--------|--------|
| `docs/security.md` | Removed URL validation claim | Feature not implemented — no code blocks javascript:/file:/ftp: schemes |
| `docs/before-you-ship.md` | Fixed JWT env var to `JWT_SECRET_KEY`, key length to "minimum 32 chars" | Users interact with compose.env which uses `JWT_SECRET_KEY`, not the ASP.NET config key |
| `docs/features.md` | Updated email description with Fluid engine and all 4 templates | Was outdated after Fluid template system was added |
| `src/backend/AGENTS.md` | Added `ErrorType.Forbidden` (403) to mapping table | Was missing from the 4-value enum documentation |
| `src/backend/AGENTS.md` | Fixed pagination signature to `Paginate(int, int)` → `IQueryable<T>` | Was incorrectly documented as `Paginate(PaginatedRequest)` → `PaginatedResponse<T>` |
| `src/backend/AGENTS.md` | Fixed BaseEntity ctor description | Was "public ctor", actually protected init pattern for derived entities |
| `src/backend/AGENTS.md` | Clarified `BaseEntityConfiguration<T>` is public, derived configs internal | Was incorrectly described as internal |
| `src/backend/AGENTS.md` | Replaced non-existent feature repository path with Identity namespace | `I{Feature}Repository.cs` pattern was never instantiated |
| `src/frontend/AGENTS.md` | Fixed routing diagram for 503 handling and login redirect | `(public)` layout also throws 503; redirect is page-level |
| `SKILLS.md` | Clarified rate limit class nesting inside `RateLimitingOptions` | Recipe showed standalone class, but it must be nested |
| `SKILLS.md` | Fixed docker-compose overlay filenames | `local.yml` → `docker-compose.local.yml` |
| `SKILLS.md` | Replaced non-existent `x-frontend-environment` anchor | Anchor doesn't exist; frontend env is inline in service block |
| `SKILLS.md` | Updated `actions/checkout@v4` to `@v6` | CI uses v6 |
| `SKILLS.md` | Added `timeline` to shadcn component list | Component exists but wasn't listed |
| `SKILLS.md` | Fixed fire-and-forget job example to use `ITemplatedEmailSender` | Was using non-existent `IEmailService.SendWelcomeAsync` |
| `FILEMAP.md` | Added change impact entries for ICacheService, CacheKeys, CachingOptions, ICookieService, CookieNames, IUserService, IUserContext, EmailTemplateNames | High-impact interfaces had no documentation |
| `FILEMAP.md` | Added non-Features/ directories to backend naming patterns | Identity, Caching, Cookies, Persistence namespaces were missing |
| `README.md` | Added pnpm prerequisite with corepack hint | Init script checks for pnpm and fails without it |
| `README.md` | Updated SKILLS.md recipe count to 35+ | Was "25+" but 35 recipes actually exist |
| `CONTRIBUTING.md` | Removed "no skipped tests" rule | 14 tests legitimately skipped for Testcontainers (#174) |

## Decisions & Reasoning

### Remove URL validation claim vs. implement it

- **Choice**: Remove the claim from docs/security.md
- **Alternatives considered**: Implementing URL scheme validation
- **Reasoning**: The feature doesn't exist in the codebase. Documenting non-existent features is worse than not documenting them. Implementation can come later as a separate PR.

### Remove "no skipped tests" rule vs. qualify it

- **Choice**: Remove the rule entirely from CONTRIBUTING.md
- **Alternatives considered**: Qualifying with exceptions for Testcontainers, keeping as aspirational
- **Reasoning**: 14 legitimately skipped tests exist, tracked by #174. The rule was unenforceable and misleading. Better to remove than to have a rule that contradicts reality.

### JWT env var: fix docs vs. fix code

- **Choice**: Fix the docs to reference `JWT_SECRET_KEY` (what users set in compose.env)
- **Alternatives considered**: Renaming the compose env var to match the ASP.NET config key
- **Reasoning**: The docker-compose.yml mapping from `JWT_SECRET_KEY` → `Authentication__Jwt__Key` is the standard pattern. Users interact with compose.env, so docs should reference what they see there.

## Follow-Up Items

- [ ] Consider implementing URL scheme validation (removed from security.md but could be valuable)
- [ ] Address skipped tests when Testcontainers support is added (#174)
