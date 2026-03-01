# MailPit Integration for Local Email Testing

**Date**: 2026-03-01
**Scope**: Replace NoOpEmailService with real SMTP delivery via MailKit, add MailPit to Aspire for local email testing

## Summary

Added an `Enabled` toggle to `EmailOptions` that switches between `NoOpEmailService` (log only) and a new `SmtpEmailService` (MailKit). Integrated MailPit into the Aspire AppHost so local dev gets a web mailbox UI for visually verifying email templates. Updated init scripts for the expanded port range.

## Changes Made

| File | Change | Reason |
|------|--------|--------|
| `Directory.Packages.props` | Added `MailKit` and `CommunityToolkit.Aspire.Hosting.MailPit` | SMTP client and Aspire MailPit container |
| `MyProject.Infrastructure.csproj` | Added `MailKit` reference | Infrastructure needs SMTP client |
| `MyProject.AppHost.csproj` | Added `CommunityToolkit.Aspire.Hosting.MailPit` reference | AppHost needs MailPit hosting |
| `EmailOptions.cs` | Added `Enabled` flag, implemented `IValidatableObject` | Conditionally require SMTP host when enabled |
| `SmtpEmailService.cs` | New file - MailKit SMTP sender | Real email delivery for dev/production |
| `ServiceCollectionExtensions.cs` (email) | Conditional registration based on `Enabled` | SmtpEmailService when enabled, NoOpEmailService when disabled |
| `appsettings.json` | Added `"Enabled": false` to Email section | Safe production default |
| `appsettings.Development.json` | Added `"Enabled": true` + MailPit SMTP defaults | Dev uses MailPit automatically |
| `appsettings.Testing.json` | Added `"Enabled": false` to Email section | Tests use NoOp |
| `api.env` | Added `Email__Enabled=true` | Production template |
| `MyProject.AppHost/Program.cs` | Added MailPit resource at base+7/+8, wired SMTP env vars | Local email testing infrastructure |
| `init.sh` / `init.ps1` | Max port 65529 to 65527 | Accommodate 2 new MailPit ports |
| `SmtpEmailServiceTests.cs` | New file - invalid host and cancellation tests | Verify error propagation |
| `EmailOptionsValidationTests.cs` | Added Enabled flag validation tests | Verify conditional SMTP validation |
| `FILEMAP.md` | Added EmailOptions and IEmailService impact rows | Change impact tracking |

## Decisions & Reasoning

### Enabled flag defaults to false

- **Choice**: `Enabled = false` by default, opt-in for SMTP
- **Alternatives considered**: Default true (like Caching/Jobs)
- **Reasoning**: Unlike Caching and Jobs which have sensible defaults and work without external infrastructure, email requires real SMTP credentials. Defaulting to true with an empty host would crash on startup validation. Safe-by-default.

### Connect/disconnect per invocation

- **Choice**: SmtpEmailService creates a new SMTP connection for each send
- **Alternatives considered**: Persistent connection pool
- **Reasoning**: Service is scoped lifetime. Email sends are infrequent (registration, password reset). Connection pooling adds complexity for negligible gain. TemplatedEmailSender already swallows failures, so transient connection issues are handled.

### MailPit ports at base+7 and base+8

- **Choice**: Continue the sequential port allocation pattern
- **Alternatives considered**: Random ports, fixed well-known ports
- **Reasoning**: Consistent with existing pattern (postgres=+4, minio=+5, minioConsole=+6). Predictable, no collisions between projects.

## Follow-Up Items

- [ ] Consider adding MailPit data volume for email persistence across restarts (not critical for dev)
