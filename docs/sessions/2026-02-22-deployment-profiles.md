# Deployment Environment Profiles

**Date:** 2026-02-22
**Scope:** Deployment infrastructure, frontend runtime config
**Files changed:** 31 (647 insertions, 272 deletions)

## Summary

Overhauled the deployment setup from a flat root-level layout into a structured `deploy/` directory using Docker Compose overlay pattern. Each environment (local, production) is a self-contained profile with its own compose overlay and env file. Separated image building from environment launching. Made the Turnstile CAPTCHA key runtime-configurable for build-once-deploy-anywhere.

## Commits

| Commit | Description |
|--------|-------------|
| `refactor(frontend): make Turnstile site key runtime-configurable` | Move `PUBLIC_TURNSTILE_SITE_KEY` from `$env/static/public` (build-time) to `$env/dynamic/private` (runtime SSR). Thread as prop through layout → pages → components. Remove Dockerfile ARG and CI build-arg. |
| `refactor(deploy): restructure deployment into environment profiles` | Split monolithic compose into base + overlays. Create `deploy/` directory with `up.sh`/`build.sh`, env templates, production profile. Update init scripts and gitignore. |
| `docs: update all references for new deployment structure` | Update README, AGENTS, SKILLS, FILEMAP, CONTRIBUTING, development.md, before-you-ship.md, gen-types command. |
| `fix(deploy): harden production profile and address review feedback` | Add container hardening (cap_drop, read_only, no-new-privileges, memory limits, log rotation). Add frontend healthcheck. Fix Redis password leak in healthcheck. Remove Turnstile testing key default from base compose. |

## Architecture

```
deploy/
├── docker-compose.yml              # Base: topology, health checks, networks
├── docker-compose.local.yml        # Local: build from source, Seq, host ports
├── docker-compose.production.yml   # Production: pre-built images, hardened
├── envs/
│   ├── local.env.example           # Dev defaults (works out of the box)
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
- **Testing key only in local.env.example**, not base compose: prevents silent CAPTCHA bypass in production
- **`REDISCLI_AUTH` env var** over `-a` CLI flag: avoids password leak in process listings
