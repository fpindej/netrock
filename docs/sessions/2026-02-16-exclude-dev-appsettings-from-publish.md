# Exclude Dev/Test Appsettings from Production Artifacts

**Date**: 2026-02-16
**Scope**: Strip `appsettings.Development.json` and `appsettings.Testing.json` from `dotnet publish` output and Docker images

## Summary

Added two-layer defense to prevent non-production appsettings from shipping in production Docker images. The `.csproj` uses `CopyToPublishDirectory="Never"` to strip them at the MSBuild level, and the Dockerfile adds a `rm -f` after publish as defense-in-depth. Verified locally that only `appsettings.json` appears in the final image.

## Changes Made

| File | Change | Reason |
|------|--------|--------|
| `WebApi/MyProject.WebApi.csproj` | Added `CopyToPublishDirectory="Never"` for `appsettings.Development.json` and `appsettings.Testing.json` | Primary exclusion — works for any publish scenario (CI, manual, Docker) |
| `WebApi/Dockerfile` | Added `RUN rm -f` after `dotnet publish` for both files | Defense-in-depth — catches bypass of MSBuild exclusion |
| `src/backend/AGENTS.md` | Added "Production build hygiene" subsection under Hosting Configuration | Document the two-layer pattern and guidance for future appsettings files |
| `FILEMAP.md` | Added impact rows for `Dockerfile` and `.csproj` appsettings changes | Change impact tracking |

## Decisions & Reasoning

### Two-layer exclusion over single approach

- **Choice**: Both `.csproj` `CopyToPublishDirectory="Never"` and Dockerfile `rm -f`
- **Alternatives considered**: Only `.csproj` (simpler), only Dockerfile `rm -f` (fragile), `.dockerignore` (wrong layer — affects build context, not publish output)
- **Reasoning**: Belt and suspenders. The `.csproj` approach is the canonical MSBuild solution and works everywhere (CI pipelines, manual publish, any Dockerfile). The Dockerfile `rm -f` is cheap insurance against edge cases where the MSBuild exclusion could be bypassed (future SDK behavior changes, custom publish profiles, copy commands). Neither has downsides — `rm -f` is a no-op if the files are already absent.

### Not using .dockerignore

- **Choice**: Did not add exclusions to `.dockerignore`
- **Reasoning**: `.dockerignore` controls what enters the Docker build context, not what ends up in the published output. The dev/test appsettings need to be in the build context so `dotnet build` can resolve them during the build stage. Excluding them from `.dockerignore` would break the build. The correct layers are publish-time (`.csproj`) and post-publish (Dockerfile `rm -f`).

## Follow-Up Items

- [ ] If a new `appsettings.{Environment}.json` is ever added, evaluate whether it belongs in production and add matching exclusions if not
