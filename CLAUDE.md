# CLAUDE.md

NETrock — .NET 10 API (Clean Architecture) + SvelteKit frontend (Svelte 5), fully dockerized.

```
Frontend (SvelteKit :5173) → /api/* proxy → Backend API (.NET :8080) → PostgreSQL / Redis / Seq
Backend layers: WebApi → Application ← Infrastructure → Domain + Shared
```

## Hard Rules

### Backend

- `Result`/`Result<T>` for all fallible operations — never throw for business logic
- `TimeProvider` (injected) — never `DateTime.UtcNow` or `DateTimeOffset.UtcNow`
- C# 13 `extension(T)` syntax for new extension methods
- Never `null!` — fix the design instead
- `ProblemDetails` (RFC 9457) for all error responses — never anonymous objects or raw strings
- `internal` on all Infrastructure service implementations
- `/// <summary>` XML docs on all public and internal API surface
- `System.Text.Json` only — never `Newtonsoft.Json`
- NuGet versions in `Directory.Packages.props` only — never in `.csproj`

### Frontend

- Never hand-edit `v1.d.ts` — run `pnpm run api:generate`
- Svelte 5 Runes only — never `export let`
- `interface Props` + `$props()` — never `$props<{...}>()`
- Logical CSS only: `ms-*`/`me-*`/`ps-*`/`pe-*` — never `ml-*`/`mr-*`/`pl-*`/`pr-*`
- No `any` — define proper interfaces
- Feature folders in `$lib/components/{feature}/` with barrel `index.ts`

### Cross-Cutting

- Security restrictive by default — deny first, open selectively
- Atomic commits: `type(scope): imperative description` (Conventional Commits)

## Verification

Run before every commit. Fix all errors before committing. **Loop until green — never commit with failures.**

```bash
# Backend (run when src/backend/ changed)
dotnet build src/backend/MyProject.slnx && dotnet test src/backend/MyProject.slnx -c Release

# Frontend (run when src/frontend/ changed)
cd src/frontend && pnpm run test && pnpm run format && pnpm run lint && pnpm run check
```

## Autonomous Behaviors

Do these automatically — never wait to be asked:

| Trigger | Action |
|---|---|
| **Any code change** | Run relevant verification (backend/frontend/both). Fix failures. Loop until green. |
| **Modifying existing files** | Check FILEMAP.md for downstream impact before editing. Update all affected files in the same commit. |
| **Backend API change** (endpoint, DTO, response shape) | Regenerate frontend types (`pnpm run api:generate`), fix type errors. |
| **Logically complete unit of work** | Commit immediately with Conventional Commit message. Don't batch, don't ask. |
| **Creating a PR** (`/create-pr`) | Auto-generate session doc in `docs/sessions/` for non-trivial PRs (3+ commits or 5+ files). |
| **Adding any feature** | Write tests alongside the implementation — component, API integration, validator as applicable. |
| **Build/test failure** | Read the error, fix it, re-run. Repeat until green. Don't stop and report the error unless stuck after 3 attempts. |
| **Unclear requirement** | Infer from context and existing patterns first. Ask the user only when genuinely ambiguous (multiple valid approaches with different tradeoffs). |

## File Roles

| File | Contains |
|---|---|
| `AGENTS.md` | Architecture, security, code quality, git workflow |
| `src/backend/AGENTS.md` | Backend conventions: entities, Result, EF Core, controllers, auth, testing |
| `src/frontend/AGENTS.md` | Frontend conventions: API client, components, styling, routing, i18n |
| `SKILLS.md` | Step-by-step recipes for all common operations |
| `FILEMAP.md` | "When you change X, also update Y" — change impact tables |
