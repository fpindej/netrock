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
- **Build script**: `deploy/build.sh` - builds, tags, pushes images with version management
- **Env templates**: `deploy/envs/production-example/` - reference config for any deployment target

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
- [ ] All required env vars documented in `deploy/envs/production-example/`
- [ ] Sensitive values (passwords, keys, tokens) not committed - only examples/placeholders
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

### Build & Release
- [ ] `deploy/config.json` has correct registry, image names, platform
- [ ] Version bumping works (patch/minor/major)
- [ ] Build script finds WebApi directory and Dockerfile dynamically
- [ ] Images tagged with both version and `:latest`
- [ ] Build context is minimal (only src/backend or src/frontend)

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
