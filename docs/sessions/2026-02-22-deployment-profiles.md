# Deployment Environment Profiles

**Date:** 2026-02-22
**Scope:** Deployment infrastructure, frontend runtime config
**Files changed:** 36 (~700 insertions, ~280 deletions)

## Summary

Overhauled the deployment setup from a flat root-level layout into a structured `deploy/` directory using Docker Compose overlay pattern. Each environment (local, production) is a self-contained profile with its own compose overlay and env file. Separated image building from environment launching. Made the Turnstile CAPTCHA key runtime-configurable for build-once-deploy-anywhere.

## Commits

| Commit | Description |
|--------|-------------|
| `refactor(frontend): make Turnstile site key runtime-configurable` | Move `PUBLIC_TURNSTILE_SITE_KEY` from `$env/static/public` (build-time) to `$env/dynamic/private` (runtime SSR). Thread as prop through layout → pages → components. Remove Dockerfile ARG and CI build-arg. |
| `refactor(deploy): restructure deployment into environment profiles` | Split monolithic compose into base + overlays. Create `deploy/` directory with `up.sh`/`build.sh`, env templates, production profile. Update init scripts and gitignore. |
| `docs: update all references for new deployment structure` | Update README, AGENTS, SKILLS, FILEMAP, CONTRIBUTING, development.md, before-you-ship.md, gen-types command. |
| `fix(deploy): harden production profile and address review feedback` | Add container hardening (cap_drop, read_only, no-new-privileges, memory limits, log rotation). Add frontend healthcheck. Fix Redis password leak in healthcheck. Remove Turnstile testing key default from base compose. |
| `fix(security): isolate frontend env, add USER directives and container hardening` | Remove env_file from frontend (prevents secret leakage), add USER directives to Dockerfiles, add pids_limit, CPU limits, restart:always for production. |
| `refactor(frontend): rename turnstile key and scope to public routes` | Rename `PUBLIC_TURNSTILE_SITE_KEY` → `TURNSTILE_SITE_KEY`, scope to `(public)` layout only. |
| `fix(deploy): harden scripts and add operational comments` | `set -euo pipefail`, PowerShell exit code propagation, REDISCLI_AUTH/redis comments, legacy .env gitignore. |
| `docs: add TLS, backup, resource tuning, and volume migration guidance` | TLS termination guide with Caddy example, pg_dump backup command, resource limit tuning note, volume migration instructions. |
| `refactor(deploy): replace redis CLI password with config file generation` | Pass Redis password as env var, generate `/tmp/redis.conf` at startup via `printf`+`exec`. Process listing no longer shows the literal password. |
| `feat(deploy): segment networks into frontend and backend tiers` | Replace flat `app` network with `frontend` (frontend ↔ api) and `backend` (api ↔ db/redis/seq). Explicit `name:` with project slug for multi-tenant host safety. |
| `refactor(deploy): replace curl with .NET health probe binary` | Remove `curl` from production API image. Add minimal `HealthProbe` console app (zero NuGet deps) for Docker healthcheck. |

## Architecture

```
deploy/
├── docker-compose.yml              # Base: topology, health checks, networks
├── docker-compose.local.yml        # Local: build from source, Seq, host ports
├── docker-compose.production.yml   # Production: pre-built images, hardened
├── envs/
│   ├── local.env                   # Dev defaults (committed, works out of the box)
│   └── production.env.example      # Production template (documented placeholders)
├── build.sh / build.ps1            # Build + tag + push images
├── up.sh / up.ps1                  # Environment launcher
└── config.json                     # Registry/version config
```

**Usage:** `./deploy/up.sh local up -d --build` / `./deploy/up.sh production up -d`

## Key decisions

- **Compose overlay pattern** over single-file-with-profiles: cleaner separation, standard Docker pattern, each file has one reason to exist
- **Turnstile key as runtime SSR prop** over build-time static: enables single image across environments
- **YAML anchor `x-hardened`** in production overlay: DRY container hardening (cap_drop, read_only, no-new-privileges, memory limits)
- **Thin `up.sh` wrapper** over fat orchestration script: resolves paths, validates, then `exec docker compose`
- **Testing key only in local.env**, not base compose: prevents silent CAPTCHA bypass in production
- **Redis password via config file** over `--requirepass` CLI flag: password invisible in `ps aux` / `/proc/pid/cmdline`. `exec redis-server` for proper signal handling
- **Frontend env isolation**: frontend container receives only the vars it needs via compose `environment:` block, never the full env file with backend secrets
- **Non-root containers**: `USER node` (frontend) and `USER $APP_UID` (backend) for defense-in-depth
- **`TURNSTILE_SITE_KEY`** renamed from `PUBLIC_TURNSTILE_SITE_KEY`: the `PUBLIC_` prefix was a SvelteKit `$env/static/public` convention that no longer applies
- **Turnstile scoped to `(public)` layout**: key only serialized into login/register/forgot-password pages, not every page
- **Two-tier network segmentation** over flat network: frontend cannot reach DB/Redis directly. Slug-prefixed network names for multi-tenant Docker host safety
- **HealthProbe .NET console app** over `curl` in production image: removes attack surface (curl is a powerful exfiltration tool), uses `dotnet` already present in the image. Zero NuGet dependencies
