# EmailToken Cleanup Job

**Date**: 2026-02-17
**Scope**: Add a recurring Hangfire job to purge expired and used EmailToken rows

## Summary

Added `ExpiredEmailTokenCleanupJob` — an hourly recurring job that deletes email tokens that are either past their expiration (with a 1-hour grace period) or already consumed. The implementation clones the existing `ExpiredRefreshTokenCleanupJob` pattern exactly.

## Changes Made

| File | Change | Reason |
|------|--------|--------|
| `Infrastructure/.../RecurringJobs/ExpiredEmailTokenCleanupJob.cs` | New `internal sealed` job implementing `IRecurringJobDefinition` | Prevent unbounded EmailToken table growth |
| `Infrastructure/.../Extensions/ServiceCollectionExtensions.cs` | Registered job as scoped + `IRecurringJobDefinition` | Wire into Hangfire recurring job pipeline |

## Decisions & Reasoning

### Clone the RefreshToken cleanup pattern

- **Choice**: Mirror `ExpiredRefreshTokenCleanupJob` exactly — same structure, same 1-hour grace period, same `ExecuteDeleteAsync` bulk delete
- **Alternatives considered**: Custom retention policies, configurable grace periods
- **Reasoning**: Consistency with the existing pattern keeps the codebase predictable. The 1-hour grace period prevents the same race condition (token loaded by auth service but not yet persisted while cleanup deletes the row).

### No `IsInvalidated` condition

- **Choice**: Delete where `ExpiresAt < cutoff || IsUsed` (no `IsInvalidated` check)
- **Alternatives considered**: Adding an `IsInvalidated` flag to EmailToken
- **Reasoning**: Unlike RefreshTokens, EmailTokens don't have an invalidation concept — they're single-use and expire. `IsUsed` and `ExpiresAt` cover all cleanup scenarios.

### Skip component tests for now

- **Choice**: Defer tests until test containers are introduced
- **Alternatives considered**: Adding SQLite as a test provider for `ExecuteDeleteAsync` support
- **Reasoning**: The InMemory provider doesn't support `ExecuteDeleteAsync`. Adding SQLite solely for one test class introduces package sprawl that will be superseded by test containers.

## Follow-Up Items

- [ ] Add component tests once test containers are in place (InMemory provider doesn't support `ExecuteDeleteAsync`)
