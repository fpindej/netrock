---
description: Add a Hangfire background job (recurring or one-time)
user_invocable: true
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

For ad-hoc background work (send email, call API, process file), use `IBackgroundJobClient` directly.

**1. Create the job class** in `src/backend/MyProject.Infrastructure/Features/Jobs/`:

```csharp
internal sealed class WelcomeEmailJob(
    ITemplatedEmailSender templatedEmailSender,
    ILogger<WelcomeEmailJob> logger)
{
    public async Task ExecuteAsync(string userId, string email)
    {
        await templatedEmailSender.SendSafeAsync("welcome", new WelcomeModel(email), email, default);
        logger.LogInformation("Sent welcome email to user '{UserId}'", userId);
    }
}
```

All parameters must be **JSON-serializable** (Hangfire persists them). Never pass `IServiceProvider`, `HttpContext`, or `DbContext` as arguments.

**2. Register:** `services.AddScoped<WelcomeEmailJob>();`

**3. Enqueue:**

```csharp
// Fire-and-forget
backgroundJobClient.Enqueue<WelcomeEmailJob>(job => job.ExecuteAsync(user.Id, user.Email));

// Delayed
backgroundJobClient.Schedule<WelcomeEmailJob>(job => job.ExecuteAsync(user.Id, user.Email), TimeSpan.FromMinutes(30));
```
