---
disable-model-invocation: true
---

Adds an environment variable (backend or frontend).

## Backend-consumed variable

1. Add to `appsettings.Development.json` if it needs a dev-specific value (Aspire injects infrastructure connection strings automatically - only add behavioral config here)
2. Add to `deploy/envs/production-example/api.env` with a placeholder for production
3. If it maps to an Options class: use `Section:Key` naming in appsettings (e.g., `Authentication:Jwt:AccessTokenLifetime`)
4. If it needs Aspire wiring for local dev: add via `.WithEnvironment()` in `MyProject.AppHost/Program.cs`
5. If it needs an Options class: use `/add-options-class`

## Frontend-consumed variable

1. Add to `src/frontend/.env.example` (documentation with placeholder)
2. Add to `src/frontend/.env.test` (valid test value for CI)
3. Add to `src/frontend/src/lib/config/server.ts` (server-only) or `i18n.ts` (client-safe)
4. Never export server config from the barrel (`$lib/config/index.ts`)

## Frontend `PUBLIC_*` variable (SvelteKit `$env/static/public`)

1. Steps 1-2 above
2. Add `ARG` + `ENV` to `src/frontend/Dockerfile` (before `pnpm run build`)
3. Add `--build-arg` to `deploy/build.sh`, `deploy/build.ps1`, and `.github/workflows/docker.yml`
4. Import: `import { PUBLIC_VAR } from '$env/static/public';`

> For secrets or keys that differ per environment (like Turnstile site keys), prefer runtime configuration via `$env/dynamic/private` with SSR layout data instead of build-time `PUBLIC_*` args.
