# Fix Hangfire Static API Crash in Production

**Date**: 2026-02-16
**Scope**: Replace Hangfire static `RecurringJob.*` and `JobStorage.Current` APIs with DI-based alternatives

## Summary

Hangfire's static `RecurringJob.AddOrUpdate` and `JobStorage.Current` APIs rely on `JobStorage.Current` being initialized, which only happens implicitly when `UseHangfireDashboard` is called. Since the dashboard is gated behind `env.IsDevelopment()`, production environments crash with `InvalidOperationException` at startup. Fixed by injecting `IRecurringJobManager` and `JobStorage` from DI in both `ApplicationBuilderExtensions` and `JobManagementService`.

## Changes Made

| File | Change | Reason |
|------|--------|--------|
| `Jobs/Extensions/ApplicationBuilderExtensions.cs` | Resolve `IRecurringJobManager` from DI; pass to `RegisterRecurringJobs` and `RestorePauseStateAsync` | Static `RecurringJob.AddOrUpdate` crashes when `JobStorage.Current` is unset |
| `Jobs/Services/JobManagementService.cs` | Inject `IRecurringJobManager` and `JobStorage` via constructor; replace all `RecurringJob.*` static calls and `JobStorage.Current` references | Same root cause — all static APIs depend on `JobStorage.Current` |

## Decisions & Reasoning

### Fix JobManagementService beyond what the issue requested

- **Choice**: Fixed both `ApplicationBuilderExtensions` (per issue) and `JobManagementService` (beyond issue scope)
- **Alternatives considered**: Only fixing the startup code per the issue
- **Reasoning**: `JobManagementService` uses `RecurringJob.TriggerJob`, `RecurringJob.RemoveIfExists`, `RecurringJob.AddOrUpdate`, and `JobStorage.Current.GetConnection()` — all of which share the same root cause. Fixing only the startup code would leave the admin job management API broken in production.

### DI injection over manual JobStorage.Current initialization

- **Choice**: Inject `IRecurringJobManager` and `JobStorage` from DI
- **Alternatives considered**: Manually calling `JobStorage.Current = ...` at startup before job registration
- **Reasoning**: DI injection is the idiomatic Hangfire approach for applications using `AddHangfire`. The static API exists for backwards compatibility. Manual initialization of `JobStorage.Current` is fragile and couples the code to Hangfire internals.

## Follow-Up Items

- [ ] Verify in production-like environment with `ASPNETCORE_ENVIRONMENT=Production` and dashboard disabled
