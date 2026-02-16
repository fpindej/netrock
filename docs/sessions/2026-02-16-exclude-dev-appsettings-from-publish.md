# Exclude Dev/Test Appsettings from Production Artifacts

**Date**: 2026-02-16
**Scope**: Strip `appsettings.Development.json` and `appsettings.Testing.json` from production publish output and Docker images while preserving them for local Docker development

## Summary

Added two-layer defense to prevent non-production appsettings from shipping in production Docker images, controlled by a single `StripDevConfig` build arg/MSBuild property. Production builds (default) exclude dev/test config from both the `dotnet publish` output and the Docker image. Local Docker dev (`docker-compose.local.yml`) passes `STRIP_DEV_CONFIG=false` to preserve all appsettings.

## Changes Made

| File | Change | Reason |
|------|--------|--------|
| `WebApi/MyProject.WebApi.csproj` | `CopyToPublishDirectory="Never"` conditional on `StripDevConfig != false` | Primary exclusion — strips dev/test config at MSBuild level |
| `WebApi/Dockerfile` | `STRIP_DEV_CONFIG` build arg (default: `true`), passed to `dotnet publish` as `/p:StripDevConfig`, conditional `rm -f` after publish | Defense-in-depth + single control point for both layers |
| `docker-compose.local.yml` | `args: STRIP_DEV_CONFIG: "false"` in api build config | Local dev retains all appsettings files |
| `src/backend/AGENTS.md` | "Production build hygiene" subsection documenting the pattern | Guide for future appsettings additions |
| `FILEMAP.md` | Impact rows for `Dockerfile` and `.csproj` appsettings changes | Change impact tracking |

## Decisions & Reasoning

### Two-layer exclusion with unified control

- **Choice**: Both `.csproj` `CopyToPublishDirectory="Never"` and Dockerfile `rm -f`, controlled by a single `StripDevConfig` flag
- **Alternatives considered**: Only `.csproj` (simpler but no Dockerfile safety net), only Dockerfile `rm -f` (fragile), separate `Dockerfile.local` (more files to maintain), `.dockerignore` (wrong layer)
- **Reasoning**: Belt and suspenders. The `.csproj` is the canonical MSBuild solution. The Dockerfile `rm -f` catches edge cases. A single build arg flows through both layers, keeping the control surface minimal. `docker-compose.local.yml` opts out with one line.

### Build arg over separate Dockerfile

- **Choice**: Single Dockerfile with `STRIP_DEV_CONFIG` build arg
- **Alternatives considered**: `Dockerfile.local` (like the frontend has), environment-based condition
- **Reasoning**: The frontend uses `Dockerfile.local` because its local dev setup is fundamentally different (volume mounts for HMR). The backend Dockerfile is the same structure for both — only the config inclusion differs. A build arg is the right abstraction for a single toggle.

### Not using .dockerignore

- **Choice**: Did not add exclusions to `.dockerignore`
- **Reasoning**: `.dockerignore` controls what enters the Docker build context, not what ends up in the published output. The dev/test appsettings need to be in the build context so `dotnet build` can resolve them during the build stage. The correct layers are publish-time (`.csproj`) and post-publish (Dockerfile `rm -f`).

## Follow-Up Items

- [ ] If a new `appsettings.{Environment}.json` is ever added, evaluate whether it belongs in production and add matching exclusions if not
