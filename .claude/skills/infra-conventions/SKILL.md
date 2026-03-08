---
description: "Infrastructure and deployment conventions. Auto-injected into devops-aware agents - not user-invocable."
user-invocable: false
---

# Infrastructure Conventions

## Overview

```
Local dev:  Aspire AppHost -> PostgreSQL, MinIO, MailPit (containers) + API + Frontend (processes)
Production: User's choice - Docker Compose, Coolify, Railway, Kubernetes, etc.
```

- **Backend Dockerfile**: Multi-stage .NET build at `src/backend/MyProject.WebApi/Dockerfile`
- **Frontend Dockerfile**: Multi-stage Node/SvelteKit build at `src/frontend/Dockerfile`
- **Production config**: `appsettings.json` defines the full config structure; `docs/before-you-ship.md` lists what to configure

## Deployment Checklist

### Dockerfile Integrity
- [ ] All `.csproj` files referenced by WebApi have COPY lines in the restore layer
- [ ] Multi-stage build: restore -> build -> publish -> final (no SDK in final image)
- [ ] `StripDevConfig` removes `appsettings.Development.json` and `appsettings.Testing.json`
- [ ] Health probe binary published separately and copied to final image
- [ ] Non-root user (`$APP_UID`) in final stage
- [ ] No secrets baked into image (build args, env vars at build time)
- [ ] `.dockerignore` excludes unnecessary files (bin, obj, node_modules, .git)

### Environment Variables
- [ ] All configurable options have sensible defaults in `appsettings.json`
- [ ] Sensitive values use placeholders in base config (not real secrets)
- [ ] Connection strings use env var substitution, not hardcoded values
- [ ] `ASPNETCORE_ENVIRONMENT` set to `Production`

### Aspire (Local Dev)
- [ ] AppHost references all infrastructure dependencies
- [ ] Port allocation follows the project convention (base+N pattern)
- [ ] Credentials pinned via parameters (not randomly generated)
- [ ] `WithDataVolume()` on stateful resources for persistence across restarts
- [ ] ServiceDefaults wired: OTEL, service discovery, resilience
- [ ] Graceful degradation when not running under Aspire

### Health Checks
- [ ] API: `/health/live` (liveness) and `/health/ready` (readiness)
- [ ] Health probe binary used in Docker (not curl/wget which add attack surface)
- [ ] Frontend: HTTP check on port 3000
- [ ] Start periods appropriate for cold starts (API: 60s, DB: 15s)

### Reproducibility
- [ ] Can clone and run with `dotnet run --project src/backend/MyProject.AppHost` (local dev)
- [ ] No machine-specific paths or assumptions
- [ ] NuGet versions pinned in `Directory.Packages.props`
- [ ] pnpm lockfile committed

### Security (Infrastructure)
- [ ] Production containers should use read-only root filesystem where possible
- [ ] `no-new-privileges:true` and `cap_drop: ALL` recommended
- [ ] tmpfs for writable directories (.NET needs /tmp and /home/app)
- [ ] Database not exposed on host network in production
- [ ] MinIO credentials not using defaults
