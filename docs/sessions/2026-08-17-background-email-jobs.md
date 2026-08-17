# Resilient Email Delivery via Hangfire Background Jobs

**Date**: 2026-08-17
**Scope**: Route transactional emails through Hangfire so SMTP failures are retried instead of silently swallowed (issue #348).

## Summary

`TemplatedEmailSender.SendSafeAsync` swallowed SMTP exceptions, so verification, password-reset, invitation and 2FA emails could be lost without any recovery. Emails are now queued as Hangfire jobs (`EmailDeliveryJob`) whenever both `Email:Enabled` and `JobScheduling:Enabled` are `true`; the job runs `SmtpEmailService` under `[AutomaticRetry]` with 5 attempts and 30s/2m/10m/30m/1h backoff, and exhausted retries stay visible as failed jobs in the Hangfire dashboard. Email-only configurations still send directly, disabled email keeps the no-op path.

## Changes Made

| File | Change | Reason |
|------|--------|--------|
| `src/backend/MyProject.Infrastructure/Features/Email/Jobs/EmailDeliveryJob.cs` | New Hangfire job executing `SmtpEmailService.SendEmailAsync` with `[AutomaticRetry]` | Retry/backoff/dead-letter via Hangfire, no custom outbox |
| `src/backend/MyProject.Infrastructure/Features/Email/Services/BackgroundEmailService.cs` | New `IEmailService` that enqueues `EmailDeliveryJob` with the rendered `EmailMessage` | Decouple request path from SMTP availability |
| `src/backend/MyProject.Infrastructure/Features/Email/Extensions/ServiceCollectionExtensions.cs` | Reads `JobScheduling:Enabled`; registers background, direct SMTP or no-op sender | Routing decision in one place |
| `src/backend/MyProject.Infrastructure/Features/Email/Services/TemplatedEmailSender.cs` | XML doc updated | Describe enqueue vs direct hand-off |
| `src/backend/tests/MyProject.Component.Tests/Services/BackgroundEmailServiceTests.cs` | Enqueue, cancellation and Hangfire serialization round-trip tests | Guard job args stay serializable |
| `src/backend/tests/MyProject.Component.Tests/Services/EmailDeliveryJobTests.cs` | Retry attribute shape and exception propagation tests | Ensure Hangfire can retry |
| `src/backend/tests/MyProject.Component.Tests/Extensions/EmailServiceRegistrationTests.cs` | DI routing tests (background / direct / no-op) | Cover the configuration matrix |
| `FILEMAP.md`, `docs/features.md`, `docs/before-you-ship.md`, `docs/architecture.md`, `README.md`, `.claude/skills/add-background-job/SKILL.md` | Document the new delivery path and one-time job conventions | Keep docs and skills accurate |

## Decisions & Reasoning

### Dedicated `EmailDeliveryJob` instead of enqueuing `SmtpEmailService` directly

- **Choice**: A thin `internal sealed EmailDeliveryJob(SmtpEmailService)` carrying the `[AutomaticRetry]` attribute.
- **Alternatives considered**: `Enqueue<SmtpEmailService>(s => s.SendEmailAsync(...))` with the attribute on `SmtpEmailService`.
- **Reasoning**: Keeps `SmtpEmailService` a pure sender usable on the direct path, gives the dashboard a meaningful job name, and keeps Hangfire concerns in one class.

### Job argument is the rendered `EmailMessage`

- **Choice**: Render synchronously in `TemplatedEmailSender`, enqueue the resulting `EmailMessage` (To, Subject, HtmlBody, PlainTextBody).
- **Alternatives considered**: Enqueue template name + model and render inside the job.
- **Reasoning**: Template/model errors surface immediately in the calling request log, job payloads are a simple record that Hangfire's Newtonsoft serializer round-trips (covered by a test), and no generic model registry is needed.

### Retry policy as constants, not options

- **Choice**: `Attempts = 5`, `DelaysInSeconds = [30, 120, 600, 1800, 3600]`, `OnAttemptsExceeded = Fail`.
- **Alternatives considered**: Configurable retry options.
- **Reasoning**: Attribute arguments must be compile-time constants; a sensible default avoids an options class nobody tunes. Failed jobs can be retried from the dashboard.

### No extra logging inside the job

- **Choice**: Let exceptions propagate; Hangfire's `AutomaticRetry` already logs each failed attempt with the exception.
- **Reasoning**: Avoids duplicate log lines and keeps PII exposure identical to the existing `SmtpEmailService` log (recipient + subject).

### Rendered payload persisted in Hangfire storage

- **Choice**: Accept that the rendered email (including verification/reset links) lives in the `hangfire` schema until the job expires (24h after success by default; failed jobs until deleted).
- **Alternatives considered**: Encrypting the payload or enqueuing only a template name + model.
- **Reasoning**: The same links already sit in the `EmailTokens` table of the same database; the built-in dashboard is development-only, and `docs/before-you-ship.md` calls out the storage sensitivity so operators lock down database access.

## Verification

- `dotnet build src/backend/MyProject.slnx` (0 warnings) and `dotnet test src/backend/MyProject.slnx -c Release` green (97 unit, 10 architecture, 554 component, 394 API tests).
- Fresh-init check: copied the branch to a scratch directory, ran `./init.sh --name Mailjob --port 15000 --yes --no-commit --no-aspire`; the renamed project built with 0 warnings and all tests passed.
- Runtime check under Aspire on the initialized project: `POST /api/auth/password/forgot` for the seeded superuser returned 200, the Hangfire dashboard showed job #1 `EmailDeliveryJob.ExecuteAsync` as Succeeded (0 failed), and MailPit received "Reset Your Password".

## Diagrams

```mermaid
flowchart TD
    A[Service calls ITemplatedEmailSender.SendSafeAsync] --> B[FluidEmailTemplateRenderer renders EmailMessage]
    B --> C{IEmailService registration}
    C -->|Email + Jobs enabled| D[BackgroundEmailService.Enqueue EmailDeliveryJob]
    D --> E[(Hangfire PostgreSQL storage)]
    E --> F[EmailDeliveryJob.ExecuteAsync]
    F -->|ok| G[SmtpEmailService sends via SMTP]
    F -->|throws| H[AutomaticRetry: 30s, 2m, 10m, 30m, 1h]
    H -->|attempts exceeded| I[Failed job in Hangfire dashboard]
    C -->|Email enabled, Jobs disabled| G
    C -->|Email disabled| J[NoOpEmailService logs only]
```

## Follow-Up Items

- [ ] Consider a dedicated Hangfire queue for email if other one-time jobs start competing for workers.
