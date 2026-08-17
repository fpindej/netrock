# Dependency Updates (August 2026)

**Date**: 2026-08-17
**Scope**: Bring all NuGet, npm and GitHub Actions dependencies to their latest versions, superseding the open Dependabot PRs.

## Summary

Updated every backend NuGet package, every frontend npm package and every GitHub Action to the latest release, including several major bumps (SkiaSharp 4, NSubstitute 6, ESLint 10, Vite 8, Vitest 4, TypeScript 6). Node.js runtime moved from 22 to 24 (active LTS). Small code adaptations were needed for Vitest 4 mock semantics and the new ESLint 10 `no-useless-assignment` rule. Backend (1046 tests) and frontend (286 tests, lint, svelte-check, production build, Docker image) are all green.

## Changes Made

| File | Change | Reason |
|------|--------|--------|
| `src/backend/Directory.Packages.props` | All packages to latest; framework 10.0.11 | Security/bug fixes; supersedes Dependabot #519, #520, #521, #522, #503 |
| `.config/dotnet-tools.json` | dotnet-ef 10.0.8 -> 10.0.11 | Keep tool aligned with EF Core version |
| `src/frontend/package.json`, `pnpm-lock.yaml` | All packages to latest; pnpm 10.34.5 | Supersedes Dependabot #515 |
| `src/frontend/vite.config.ts` | Add `clearMocks: true` | Vitest 4: `restoreMocks` no longer resets `vi.fn()` call state |
| `src/frontend/src/lib/utils/crop.test.ts` | `Image` stub uses a regular function | Vitest 4: `vi.fn(arrow)` cannot be invoked with `new` |
| `src/frontend/src/lib/components/ui/sidebar/sidebar-trigger.svelte` | `bind:ref` on `<Button>` | ESLint 10 `no-useless-assignment`; the bindable `ref` was never forwarded |
| `src/frontend/src/lib/components/ui/textarea/textarea.svelte` | Reformatted | Prettier 3.9 output changed |
| `src/frontend/project.inlang/README.md` | Regenerated | Paraglide 2.24 regenerates it on compile |
| `.github/workflows/*.yml` | checkout v7, cache v6, setup-node v7, setup-dotnet v6, Node 24 | Supersedes Dependabot #501, #504, #514 |
| `src/frontend/Dockerfile`, `README.md`, `docs/troubleshooting.md` | Node 22 -> 24 | Node 24 is the active LTS; matches `@types/node` |

## Decisions & Reasoning

### TypeScript 6, not 7

- **Choice**: `typescript@~6.0.3`
- **Alternatives considered**: TypeScript 7.0.2 (native Go port / tsgo)
- **Reasoning**: svelte-check requires TS 6 *and* TS 7 installed side by side plus an experimental `--tsgo` flag. Too experimental for a template; revisit when svelte-check supports TS 7 natively.

### Stay on pnpm 10.x

- **Choice**: pnpm 10.34.5 (latest 10.x)
- **Alternatives considered**: pnpm 11.22
- **Reasoning**: pnpm 11 removes `pnpm.onlyBuiltDependencies` from `package.json` (needs `allowBuilds` in `pnpm-workspace.yaml`), stops reading non-auth settings from `.npmrc`, and enables `minimumReleaseAge`/`strictDepBuilds` by default. That is a config migration, not a version bump, and is better done as its own PR.

### Node.js 24 runtime

- **Choice**: Bump CI, Dockerfile and docs to Node 24
- **Reasoning**: Node 22 is in maintenance; 24 is the active LTS. `@types/node` pinned to `^24` to match the runtime rather than the newest 26.x line.

## Follow-Up Items

- [ ] Migrate to pnpm 11 (`pnpm-workspace.yaml`, `allowBuilds`, `.npmrc` cleanup)
- [ ] TypeScript 7 once svelte-check supports it without the dual install
- [ ] Nine pre-existing `state_referenced_locally` svelte-check warnings (unchanged by this update)
