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

Run before every commit. Fix all errors before committing.

```bash
# Backend
dotnet build src/backend/MyProject.slnx && dotnet test src/backend/MyProject.slnx -c Release

# Frontend
cd src/frontend && pnpm run format && pnpm run lint && pnpm run check
```

## File Roles

| File | Contains |
|---|---|
| `AGENTS.md` | Architecture, security, code quality, git workflow |
| `src/backend/AGENTS.md` | Backend conventions: entities, Result, EF Core, controllers, auth, testing |
| `src/frontend/AGENTS.md` | Frontend conventions: API client, components, styling, routing, i18n |
| `SKILLS.md` | Step-by-step recipes for all common operations |
| `FILEMAP.md` | "When you change X, also update Y" — change impact tables |

## Session Documentation

Only when explicitly asked: `docs/sessions/{YYYY-MM-DD}-{topic-slug}.md` per `docs/sessions/README.md`.
