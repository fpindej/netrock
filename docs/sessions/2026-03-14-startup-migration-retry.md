# Startup Migration Retry on Aspire Cold Start

**Date**: 2026-03-14
**Scope**: Fix the 1-3 error log entries on first Aspire launch caused by the API racing PostgreSQL readiness

## Summary

On first Aspire launch (empty data volume), the API would crash or log errors because `MigrateAsync()` ran before PostgreSQL was fully ready to accept SQL commands. The container reported healthy (TCP port open) but wasn't ready for query execution. Fixed with three complementary layers: a silent connection pre-check, EF Core's built-in retry strategy, and a log level correction for retried transient errors.

## Changes Made

| File | Change | Reason |
|------|--------|--------|
| `ServiceCollectionExtensions.cs` | Added `EnableRetryOnFailure()` to Npgsql provider | EF Core's built-in execution strategy retries transient DB errors with exponential backoff |
| `ServiceCollectionExtensions.cs` | Added `ConfigureWarnings(CommandError -> Warning)` | With retry active, a single command failure is transient, not terminal - prevents Error-level log entries for retried operations |
| `ApplicationBuilderExtensions.cs` | Added `WaitForDatabaseAsync` with `CanConnectAsync` loop | Pre-checks DB connectivity silently (no error logs) before running migrations. Bounded: 30 attempts, 60s max. |
| `ApplicationBuilderExtensions.cs` | Changed `Migrate()` to `MigrateAsync()` | Avoids blocking the startup thread |
| `RoleManagementService.cs` | Wrapped transaction in `CreateExecutionStrategy().ExecuteAsync()` | Required because EF Core's retry strategy does not support user-initiated transactions outside the strategy wrapper |

## Decisions & Reasoning

### Three-layer approach instead of a single fix

- **Choice**: `WaitForDatabaseAsync` + `EnableRetryOnFailure` + `ConfigureWarnings`
- **Alternatives considered**: Manual retry loop around `MigrateAsync` (rejected as messy); only `EnableRetryOnFailure` (still logs errors at Error level); only `CanConnectAsync` pre-check (doesn't handle runtime transient errors)
- **Reasoning**: Each layer handles a different concern. The pre-check prevents most startup failures silently. The retry strategy handles transient errors at runtime (not just startup). The warning downgrade prevents the Aspire dashboard from showing red badges for retried operations that succeed.

### ConfigureWarnings for CommandError instead of suppressing logs

- **Choice**: Downgrade `RelationalEventId.CommandError` (event 20102) from Error to Warning
- **Alternatives considered**: Suppress the event entirely; filter in Serilog config; accept the error logs
- **Reasoning**: Warning is semantically correct - the command failed but will be retried. Real failures still surface as thrown exceptions logged by the exception handling middleware. No visibility is lost.

### Execution strategy wrapper on SetRolePermissionsAsync

- **Choice**: Wrap the explicit transaction in `CreateExecutionStrategy().ExecuteAsync()`
- **Alternatives considered**: Remove the explicit transaction; disable retry for that operation
- **Reasoning**: EF Core throws `InvalidOperationException` when `BeginTransactionAsync` is called with a retry strategy active but outside the strategy wrapper. This is the documented pattern from Microsoft's connection resiliency guide. The operations inside (delete + insert + security stamp rotation) are idempotent, so retry is safe.

## Follow-Up Items

- [ ] Test with Aspire on a clean data volume to verify zero Error-level log entries
