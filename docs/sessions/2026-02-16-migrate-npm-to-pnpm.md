# Migrate from npm to pnpm

**Date**: 2026-02-16
**Scope**: Replace npm with pnpm as the frontend package manager, updating Dockerfiles, CI workflows, and all documentation references.

## Summary

Migrated the frontend from npm to pnpm using corepack (built into Node 22). This gives faster installs, strict dependency isolation (no phantom dependencies), and a content-addressable store that saves disk space. The migration touched Dockerfiles, CI workflows, and ~60 documentation references across 12+ files.

## Changes Made

| File | Change | Reason |
|------|--------|--------|
| `src/frontend/package.json` | Added `"packageManager": "pnpm@10.29.3"` and `pnpm.onlyBuiltDependencies` | Pin exact pnpm version via corepack; approve esbuild build script |
| `src/frontend/package-lock.json` | Deleted | Replaced by pnpm-lock.yaml |
| `src/frontend/pnpm-lock.yaml` | Generated | pnpm lockfile |
| `src/frontend/Dockerfile` | Rewritten as 3-stage build (base, prod-deps, builder, runtime) | Separate prod-deps stage replaces `npm prune --production`; BuildKit cache mounts for pnpm store |
| `src/frontend/Dockerfile.local` | Rewritten with corepack + pnpm | Match production approach |
| `.github/workflows/ci.yml` | Added `pnpm/action-setup@v4`, switched cache to pnpm, replaced npm commands | pnpm/action-setup reads packageManager field automatically |
| `.github/workflows/docker.yml` | Replaced `package*.json` glob with explicit `package.json` + `pnpm-lock.yaml` | Precise trigger paths for pnpm lockfile |
| `CLAUDE.md` | npm run commands to pnpm run | Keep pre-commit checks accurate |
| `AGENTS.md` | npm references to pnpm in pre-commit checks, Dockerfile descriptions | Consistency |
| `SKILLS.md` | All npm commands to pnpm equivalents, "Add an npm Package" to "Add a Package" | Recipes must reflect actual tooling |
| `FILEMAP.md` | npm references to pnpm | Impact tables must be accurate |
| `src/frontend/AGENTS.md` | All npm/npx commands to pnpm equivalents | Frontend conventions must reflect actual tooling |
| `src/frontend/README.md` | All npm commands to pnpm | README must reflect actual tooling |
| `src/backend/AGENTS.md` | `npm run api:generate` to `pnpm run api:generate` | Backend docs reference frontend commands |
| `.claude/commands/*.md` | All npm commands to pnpm | Agent commands must work |
| `init.sh` | Added pnpm to prerequisites check | Init script should verify pnpm is available |

## Decisions & Reasoning

### Use corepack instead of global pnpm install

- **Choice**: Pin pnpm version via `packageManager` field in package.json, activate via `corepack enable`
- **Alternatives considered**: Global `npm install -g pnpm`, standalone installer script
- **Reasoning**: Corepack is built into Node 22, ensures every developer and CI runner uses the exact same pnpm version without any extra install step. The `pnpm/action-setup@v4` GitHub Action reads the `packageManager` field automatically.

### 3-stage Dockerfile with BuildKit cache mounts

- **Choice**: Separate `base`, `prod-deps`, and `builder` stages with `--mount=type=cache,id=pnpm,target=/pnpm/store`
- **Alternatives considered**: Single-stage build with `pnpm install --prod` after build (like the old npm approach)
- **Reasoning**: Separate stages give better layer caching (prod deps don't rebuild when dev deps change). BuildKit cache mounts persist the pnpm store across builds, making rebuilds significantly faster. The `prod-deps` stage replaces the fragile `npm prune --production` pattern.

### pnpm.onlyBuiltDependencies for esbuild

- **Choice**: Add `pnpm.onlyBuiltDependencies: ["esbuild"]` to package.json
- **Alternatives considered**: `pnpm approve-builds` interactive command
- **Reasoning**: pnpm 10 requires explicit approval for packages that run postinstall scripts. esbuild needs its postinstall to download the platform-specific binary. The declarative config avoids interactive prompts in CI.

### Keep session docs unchanged

- **Choice**: Leave historical `npm` references in `docs/sessions/` files
- **Alternatives considered**: Update all session docs to reference pnpm
- **Reasoning**: Session docs are historical records of what happened during that session. Changing them would misrepresent the tooling used at the time.

## Diagrams

```mermaid
flowchart TD
    subgraph "Production Dockerfile (3-stage)"
        A[base: node:22-alpine + corepack enable] --> B[prod-deps: pnpm install --prod]
        A --> C[builder: pnpm install + pnpm run build]
        B --> D[runtime: node build/]
        C --> D
    end

    subgraph "CI Pipeline"
        E[pnpm/action-setup@v4] --> F[setup-node with pnpm cache]
        F --> G[pnpm install --frozen-lockfile]
        G --> H[lint + check]
    end
```

## Follow-Up Items

- [ ] Verify CI passes on PR #215
- [ ] Test Docker production build with `docker build -f src/frontend/Dockerfile src/frontend/ --build-arg PUBLIC_TURNSTILE_SITE_KEY=1x00000000000000000000AA`
- [ ] Test Docker local dev with `docker compose -f docker-compose.local.yml down -v && docker compose -f docker-compose.local.yml up -d frontend`
- [ ] After merge, all developers need a one-time `corepack enable` and fresh `pnpm install` in `src/frontend/`
