# Agent Guidelines

> Hard rules and verification commands are in [`CLAUDE.md`](CLAUDE.md) — always loaded.

## Architecture

```
Frontend (SvelteKit :5173)
    │  /api/* proxy (catches all, forwards cookies + headers)
    ▼
Backend API (.NET :8080)
    ├── PostgreSQL (:5432)
    ├── Redis (:6379)
    ├── Hangfire (PostgreSQL-backed)
    └── Seq (:80)
```

| Layer | Backend | Frontend |
|---|---|---|
| **Framework** | .NET 10 / C# 13 | SvelteKit / Svelte 5 (Runes) |
| **Data** | PostgreSQL + EF Core | openapi-typescript (generated types) |
| **Cache** | Redis (IDistributedCache) | — |
| **Auth** | JWT in HttpOnly cookies + permission claims | Cookie-based (automatic via proxy) |
| **Authorization** | `[RequirePermission]` + role hierarchy | `hasPermission()` utilities |
| **Validation** | FluentValidation + Data Annotations | TypeScript strict mode |
| **Styling** | — | Tailwind CSS 4 + shadcn-svelte |
| **i18n** | — | paraglide-js (compile-time) |

### Backend — Clean Architecture

```
WebApi → Application ← Infrastructure
              ↓
           Domain
All layers reference Shared (Result, ErrorType, ErrorMessages)
```

| Layer | Responsibility |
|---|---|
| **Shared** | `Result`/`Result<T>`, `ErrorType`, `ErrorMessages`, `PhoneNumberHelper`. Zero deps. |
| **Domain** | Entities (`BaseEntity`). Zero deps. |
| **Application** | Interfaces, DTOs (Input/Output), service contracts. |
| **Infrastructure** | EF Core, Identity, Redis, service implementations. All `internal`. |
| **WebApi** | Controllers, middleware, validation, request/response DTOs. Entry point. |
| **HealthProbe** | Minimal console app used as Docker health check binary (`/health/live`). |

## Code Quality

- Public methods read like a table of contents — delegate to well-named private methods.
- Extract duplication only when intent is identical and a change to one copy always means the same change to others.
- Design for testability: small focused methods, constructor injection, pure logic where possible.
- Don't wrap framework types just to mock them — use integration tests instead.

## Security

**Security is the highest priority.** When convenience and security conflict, choose security.

| Principle | Practice |
|---|---|
| Restrictive by default | Deny access, block origins, strip headers — open selectively |
| Defense in depth | Validate frontend AND backend. Auth in middleware AND controllers. |
| Least privilege | Minimum data and permissions in tokens, cookies, responses |
| Fail closed | If validation/token/origin check fails → reject. Never fall through. |
| Secrets never in code | Always `.env` or environment variables |

When building features: think about abuse first, validate all input on backend, sanitize output, protect mutations with auth + CSRF, log security events, audit significant actions via `IAuditService.LogAsync`.

## Git Workflow

**Commit continuously and atomically.** Every logically complete unit of work gets its own commit immediately — don't batch up changes.

Format: `type(scope): lowercase imperative description` — max 72 chars, no period.

```
feat(auth): add refresh token rotation
fix(profile): handle null phone number in validation
test(auth): add login integration tests
```

One commit = one logical change that could be reverted independently.

**Avoid committing broken code.** Run verification before each commit. If it fails, fix and re-run — keep the main branch green.

### Labels (Issues & PRs)

| Label | When |
|---|---|
| `backend` | Changes touch `src/backend/` |
| `frontend` | Changes touch `src/frontend/` |
| `security` | Vulnerabilities, hardening, auth features |
| `feature` | New capabilities |
| `bug` | Fixing incorrect behavior |
| `documentation` | Docs, AGENTS.md, session notes |

## Error Handling

| Layer | Strategy |
|---|---|
| Backend services | `Result.Failure(ErrorMessages.*, ErrorType.*)` |
| Backend exceptions | `KeyNotFoundException` → 404, `PaginationException` → 400, unhandled → 500 |
| Backend middleware | `ExceptionHandlingMiddleware` → `ProblemDetails` (RFC 9457) |
| Frontend errors | `getErrorMessage()` resolves `detail` → `title` → fallback |
| Frontend validation | `handleMutationError()` with `onValidationError` callback |
| Frontend rate limits | `handleMutationError()` auto-detects 429, starts cooldown |

Error messages flow: Backend `ErrorMessages.*` → `Result.Failure()` → `ProblemFactory.Create()` → `ProblemDetails.detail` → Frontend `getErrorMessage()`.

## Local Development

```bash
./deploy/up.sh local up -d --build   # Start everything (env files are committed)
```

`deploy/envs/local/` is committed with working defaults. Edit files directly to tune settings.

| File | Purpose |
|---|---|
| `deploy/envs/local/compose.env` | Docker Compose interpolation vars (DB, Redis, MinIO, JWT, ports) |
| `deploy/envs/local/api.env` | ASP.NET config overrides (auth, caching, CORS, logging, email) |
| `deploy/envs/local/seed.env` | Seed user definitions (seeded on startup) |
| `deploy/envs/production-example/` | Production template — `cp -r` to `deploy/envs/production/` |
| `deploy/docker-compose.yml` | Base service definitions |
| `deploy/docker-compose.local.yml` | Local dev overlay |
| `deploy/docker-compose.production.yml` | Production overlay |
| `appsettings.json` | Base/production defaults |
| `appsettings.Development.json` | Dev defaults (generous JWT, debug logging) |
