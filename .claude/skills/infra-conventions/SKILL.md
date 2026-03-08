---
description: "Infrastructure and deployment conventions. Auto-injected into devops-aware agents - not user-invocable."
user-invocable: false
---

# Infrastructure Conventions

## Overview

```
Local dev:  Aspire AppHost -> PostgreSQL, MinIO, MailPit (containers) + API + Frontend (processes)
Production: generated from MyProject.AppHost via ./deploy/publish.sh (aspire publish) + envs/api.env + envs/seed.env
```

- **Backend Dockerfile**: Multi-stage .NET build at `src/backend/MyProject.WebApi/Dockerfile`
- **Frontend Dockerfile**: Multi-stage Node/SvelteKit build at `src/frontend/Dockerfile`
- **Build script**: `deploy/build.sh` - builds, tags, pushes images with version management
- **Publish script**: `deploy/publish.sh` - generates production compose from Aspire AppHost
- **Launch script**: `deploy/up.sh` - thin wrapper around `docker compose` for the generated package
- **Production hardening**: read-only root, no-new-privileges, all caps dropped, resource limits, log rotation

## Deployment Checklist

### Dockerfile Integrity
- [ ] All `.csproj` files referenced by WebApi have COPY lines in the restore layer
- [ ] Multi-stage build: restore -> build -> publish -> final (no SDK in final image)
- [ ] `StripDevConfig` removes `appsettings.Development.json` and `appsettings.Testing.json`
- [ ] Health probe binary published separately and copied to final image
- [ ] Non-root user (`$APP_UID`) in final stage
- [ ] No secrets baked into image (build args, env vars at build time)
- [ ] `.dockerignore` excludes unnecessary files (bin, obj, node_modules, .git)

### Docker Compose (generated via Aspire)
- [ ] All services have health checks with appropriate intervals and retries
- [ ] Service dependencies use `condition: service_healthy`
- [ ] Volumes for persistent data (db_data, storage_data)
- [ ] Networks isolate frontend from backend (only API bridges both)
- [ ] `ApplyHardened` called on all services in `PublishAsDockerComposeService` callbacks
- [ ] Resource limits set for all services in production
- [ ] Log rotation configured (json-file driver with max-size/max-file)
- [ ] No host ports exposed for internal services (db, storage) in production

### Environment Variables
- [ ] All required env vars documented in `deploy/envs/production-example/`
- [ ] Sensitive values (passwords, keys, tokens) not committed - only examples/placeholders
- [ ] JWT secret parameter (`jwt-secret`) requires operator to fill in real value in generated `.env`
- [ ] Connection strings use env var substitution, not hardcoded values
- [ ] `ASPNETCORE_ENVIRONMENT` set to `Production` in API's publish callback

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
- [ ] Can deploy with `./deploy/publish.sh` + fill in `deploy/compose/.env` and `envs/*.env` + `./deploy/up.sh up -d`
- [ ] No machine-specific paths or assumptions
- [ ] NuGet versions pinned in `Directory.Packages.props`
- [ ] pnpm lockfile committed

### Security (Infrastructure)
- [ ] Production containers: read-only root filesystem where possible
- [ ] `no-new-privileges:true` and `cap_drop: ALL`
- [ ] PID limits set (not yet supported by Aspire Docker publisher SDK - track for future releases)
- [ ] tmpfs for writable directories (.NET needs /tmp and /home/app)
- [ ] Database not exposed on host network in production
- [ ] MinIO credentials not using defaults
