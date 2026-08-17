---
description: Add a Hangfire background job (recurring or one-time)
user-invocable: true
---

Adds a recurring or one-time Hangfire background job.

## Recurring Job

**1. Create the job class** in `src/backend/MyProject.Infrastructure/Features/Jobs/RecurringJobs/{JobName}Job.cs`:

```csharp
using Hangfire;
using Microsoft.Extensions.Logging;

namespace MyProject.Infrastructure.Features.Jobs.RecurringJobs;

/// <summary>
/// Brief description of what this job does and why.
/// </summary>
internal sealed class MyCleanupJob(
    MyProjectDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<MyCleanupJob> logger) : IRecurringJobDefinition
{
    /// <inheritdoc />
    public string JobId => "my-cleanup";

    /// <inheritdoc />
    public string CronExpression => Cron.Daily();

    /// <inheritdoc />
    public async Task ExecuteAsync()
    {
        // Job logic here - each execution gets its own DI scope
        logger.LogInformation("Job completed");
    }
}
```

Key conventions:
- Mark `internal sealed`, use primary constructor
- Descriptive `JobId` (kebab-case, e.g. `"expired-token-cleanup"`)
- `Hangfire.Cron` helpers: `Cron.Hourly()`, `Cron.Daily()`, `Cron.Weekly()`

**2. Register in DI** - add two lines to `src/backend/MyProject.Infrastructure/Features/Jobs/Extensions/ServiceCollectionExtensions.cs`:

```csharp
services.AddScoped<MyCleanupJob>();
services.AddScoped<IRecurringJobDefinition>(sp => sp.GetRequiredService<MyCleanupJob>());
```

**3. Verify:** `dotnet build src/backend/MyProject.slnx`

`UseJobScheduling()` discovers all `IRecurringJobDefinition` implementations automatically. The job appears in admin panel at `/admin/jobs`. Pause state persists to DB (`hangfire.pausedjobs`).

**Configuration** via `appsettings.json`: `"JobScheduling": { "Enabled": true, "WorkerCount": 4 }`. Dev dashboard at `http://localhost:8080/hangfire`.

## One-Time Job

For ad-hoc background work (call API, process file, generate report), use `IBackgroundJobClient` directly.

Note: transactional emails are already queued automatically - `ITemplatedEmailSender.SendSafeAsync()` routes through `BackgroundEmailService` -> `EmailDeliveryJob` (`Features/Email/Jobs/`) whenever email and job scheduling are both enabled. Do not wrap email sends in another job.

**1. Create the job class** in `src/backend/MyProject.Infrastructure/Features/{Feature}/Jobs/` (or `Features/Jobs/` for cross-cutting jobs):

```csharp
internal sealed class ReportGenerationJob(
    IReportService reportService,
    ILogger<ReportGenerationJob> logger)
{
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = [30, 120, 600])]
    public async Task ExecuteAsync(Guid reportId, CancellationToken cancellationToken)
    {
        await reportService.GenerateAsync(reportId, cancellationToken);
        logger.LogInformation("Generated report '{ReportId}'", reportId);
    }
}
```

All parameters must be **JSON-serializable** (Hangfire persists them). Never pass `IServiceProvider`, `HttpContext`, or `DbContext` as arguments. A trailing `CancellationToken` parameter is injected by Hangfire (server shutdown) - pass `CancellationToken.None` in the enqueue expression.

Let exceptions propagate - `[AutomaticRetry]` only retries when the method throws. Retries re-run the whole method, so keep it idempotent.

**2. Register:** `services.AddScoped<ReportGenerationJob>();`

**3. Enqueue:**

```csharp
// Fire-and-forget
backgroundJobClient.Enqueue<ReportGenerationJob>(job => job.ExecuteAsync(reportId, CancellationToken.None));

// Delayed
backgroundJobClient.Schedule<ReportGenerationJob>(job => job.ExecuteAsync(reportId, CancellationToken.None), TimeSpan.FromMinutes(30));
```
