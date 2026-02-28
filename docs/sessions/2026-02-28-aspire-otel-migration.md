# Aspire + OpenTelemetry Migration

**Date**: 2026-02-28
**Scope**: Migrate local development from Docker Compose to .NET Aspire, replace Serilog.Sinks.Seq with OpenTelemetry

## Summary

Added Aspire ServiceDefaults project with full OpenTelemetry instrumentation (metrics, tracing, logging) and an AppHost orchestrator that replaces Docker Compose for local development. The migration eliminates `docker-compose.local.yml`, all `deploy/envs/local/` files, and Seq — replacing them with a single `dotnet run` command that provides PostgreSQL, MinIO, API, and frontend with an integrated OTEL dashboard.

## Changes Made

### PR 1: ServiceDefaults + OTEL (`feat/service-defaults`)

| File | Change | Reason |
|------|--------|--------|
| `MyProject.ServiceDefaults/Extensions.cs` | New shared Aspire project | OTEL metrics/tracing/logging, service discovery, HTTP resilience |
| `MyProject.ServiceDefaults/MyProject.ServiceDefaults.csproj` | New project file | `IsAspireSharedProject=true` with OTEL + resilience packages |
| `Directory.Packages.props` | Add 12 packages, replace 1 | OTEL, resilience, service discovery packages; Seq → OpenTelemetry sink |
| `MyProject.Infrastructure.csproj` | `Serilog.Sinks.Seq` → `Serilog.Sinks.OpenTelemetry` | Logging via OTLP instead of Seq |
| `LoggerConfigurationExtensions.cs` | Add OTEL sink when endpoint is set | Conditional OTLP export (Aspire or production) |
| `MyProject.WebApi.csproj` | Add ServiceDefaults project reference | Wire OTEL into the API |
| `Program.cs` | Add `builder.AddServiceDefaults()` | Activate OTEL, service discovery, resilience |
| `MyProject.slnx` | Add ServiceDefaults project | Solution awareness |
| `appsettings.Development.json` | Remove Serilog Seq config | Seq package removed |

### PR 2: AppHost + Migration (`feat/aspire-local-dev`)

| File | Change | Reason |
|------|--------|--------|
| `MyProject.AppHost/Program.cs` | New Aspire orchestrator | PostgreSQL, MinIO, API, Frontend with pinned ports |
| `MyProject.AppHost/*.csproj, appsettings, launchSettings` | New project files | AppHost configuration |
| `docker-compose.local.yml` | Deleted | Replaced by Aspire |
| `deploy/envs/local/*` | Deleted (3 files) | Config moved to appsettings.Development.json |
| `appsettings.Development.json` | Major rework | Remove stale Seq/ConnectionStrings/FileStorage, add Seed users |
| `appsettings.json` | Add FileStorage placeholder | Base config for production |
| `init.sh`, `init.ps1` | Docker → Aspire | Simplified ports (2 vs 7), launch Aspire after setup |
| `deploy/up.sh`, `deploy/up.ps1` | Update docs | Clarify production-only usage |
| `vite.config.ts` | `PORT` env var support | Aspire controls frontend port |
| 13 documentation files | Seq → OTEL, Docker Compose → Aspire | Comprehensive doc migration |

## Decisions & Reasoning

### Seq → OpenTelemetry (Serilog sink)

- **Choice**: Replace `Serilog.Sinks.Seq` with `Serilog.Sinks.OpenTelemetry`
- **Alternatives considered**: Keep Seq alongside OTEL, use ILogger directly
- **Reasoning**: Aspire Dashboard provides the same structured log viewing that Seq did, and OTEL is the standard for production observability. One sink, one protocol, works everywhere.

### Session lifetime (not Persistent) for containers

- **Choice**: Use default session lifetime — containers stop on Ctrl+C
- **Alternatives considered**: `ContainerLifetime.Persistent` to keep containers running
- **Reasoning**: Persistent containers don't free system resources and cause PostgreSQL password mismatch on recreation (Aspire generates random passwords). Session lifetime + named data volumes gives the best UX: data persists, resources are freed.

### Explicit PostgreSQL password

- **Choice**: `builder.AddParameter("postgres-password", secret: true)` with dev password in appsettings.json
- **Alternatives considered**: User Secrets, auto-generated passwords
- **Reasoning**: Without explicit passwords, Aspire generates random ones each run. Existing volumes have the old password baked in during `initdb`, causing auth failures on container recreation. Explicit dev password ensures credential stability.

### Two stacked PRs

- **Choice**: Split into ServiceDefaults+OTEL (PR 1) and AppHost+migration (PR 2)
- **Alternatives considered**: Single PR, three PRs (OTEL, AppHost, docs)
- **Reasoning**: PR 1 is a clean, reviewable unit (new project + package swap). PR 2 builds on it with the full migration. Two PRs keep review scope manageable while maintaining logical cohesion.

## Diagrams

```mermaid
flowchart TD
    subgraph "Aspire AppHost (dotnet run)"
        AppHost[MyProject.AppHost]
        Dashboard[Aspire Dashboard<br/>OTEL traces/logs/metrics]
    end

    subgraph "Containers (session lifetime)"
        PG[(PostgreSQL)]
        PGA[PgAdmin]
        MINIO[MinIO]
    end

    subgraph "Projects (in-process)"
        API[MyProject.WebApi]
        FE[SvelteKit Frontend]
    end

    AppHost --> PG
    AppHost --> PGA
    AppHost --> MINIO
    AppHost --> API
    AppHost --> FE

    API -->|ConnectionStrings:Database| PG
    API -->|FileStorage__*| MINIO
    FE -->|API_URL| API

    API -->|OTLP| Dashboard
    PG -.->|health| AppHost
```

## Follow-Up Items

- [ ] Merge PR 1 (`feat/service-defaults`) first, then PR 2 (`feat/aspire-local-dev`)
- [ ] Run init script end-to-end on a clean clone to validate the full bootstrap experience
- [ ] Consider adding Redis back as an optional L2 cache backend via Aspire (if needed later)
