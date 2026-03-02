# Agent Guidelines

> Hard rules and verification commands are in [`CLAUDE.md`](CLAUDE.md) - always loaded.

## Architecture

```
Frontend (SvelteKit :5173)
    │  /api/* proxy (catches all, forwards cookies + headers)
    ▼
Backend API (.NET :8080)
    ├── PostgreSQL
    ├── Hangfire (PostgreSQL-backed)
    ├── MinIO (S3-compatible file storage)
    ├── MailPit (local email testing via Aspire)
    └── OpenTelemetry → Aspire Dashboard (local) / OTLP endpoint (production)
```

| Layer | Backend | Frontend |
|---|---|---|
| **Framework** | .NET 10 / C# 13 | SvelteKit / Svelte 5 (Runes) |
| **Data** | PostgreSQL + EF Core | openapi-typescript (generated types) |
| **Cache** | HybridCache (in-process L1) | - |
| **Auth** | JWT in HttpOnly cookies + permission claims | Cookie-based (automatic via proxy) |
| **Authorization** | `[RequirePermission]` + role hierarchy | `hasPermission()` utilities |
| **Validation** | FluentValidation + Data Annotations | TypeScript strict mode |
| **Styling** | - | Tailwind CSS 4 + shadcn-svelte |
| **i18n** | - | paraglide-js (compile-time) |

### Backend - Clean Architecture

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
| **Infrastructure** | EF Core, Identity, HybridCache, service implementations. All `internal`. |
| **WebApi** | Controllers, middleware, validation, request/response DTOs. Entry point. |
| **HealthProbe** | Minimal console app used as Docker health check binary (`/health/live`). |

## Code Quality

- Public methods read like a table of contents - delegate to well-named private methods.
- Extract duplication only when intent is identical and a change to one copy always means the same change to others.
- Design for testability: small focused methods, constructor injection, pure logic where possible.
- Don't wrap framework types just to mock them - use integration tests instead.

## Security

**Security is the highest priority.** When convenience and security conflict, choose security.

| Principle | Practice |
|---|---|
| Restrictive by default | Deny access, block origins, strip headers - open selectively |
| Defense in depth | Validate frontend AND backend. Auth in middleware AND controllers. |
| Least privilege | Minimum data and permissions in tokens, cookies, responses |
| Fail closed | If validation/token/origin check fails → reject. Never fall through. |
| Secrets never in code | Always `.env` or environment variables |

When building features: think about abuse first, validate all input on backend, sanitize output, protect mutations with auth + CSRF, log security events, audit significant actions via `IAuditService.LogAsync`.

## Git Workflow

**Commit continuously and atomically.** Every logically complete unit of work gets its own commit immediately - don't batch up changes.

Format: `type(scope): lowercase imperative description` - max 72 chars, no period.

```
feat(auth): add refresh token rotation
fix(profile): handle null phone number in validation
test(auth): add login integration tests
```

One commit = one logical change that could be reverted independently.

**Avoid committing broken code.** Run verification before each commit. If it fails, fix and re-run - keep the main branch green.

**Split large features into stacked PRs.** If a feature touches many files or layers, break it into reviewable chunks (e.g., backend models, then API endpoints, then frontend). Each PR targets the previous PR's branch via `--base`. This keeps reviews focused and mergeable. See `/create-pr` for the mechanics.

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

## Breaking Changes

When modifying existing code (not creating new), follow these rules.

### What Counts as a Breaking Change

| Layer | Breaking change |
|---|---|
| **Domain entity** | Renaming/removing a property, changing a type |
| **Application interface** | Changing a method signature, renaming/removing a method |
| **Application DTO** | Renaming/removing a field, changing nullability |
| **WebApi endpoint** | Changing route, method, request/response shape, status codes |
| **WebApi response DTO** | Renaming/removing a property, changing type or nullability |
| **Frontend API types** | Always regenerated - broken by any backend DTO change |
| **i18n keys** | Renaming a key (all usages break) |

### Safe Strategies

1. **Prefer additive changes** - add new fields/endpoints rather than removing or renaming
2. **Same-PR migration** - if a breaking change is needed, update all consumers (including frontend types) in the same PR
3. **V2 endpoint** - for significant changes, create `api/v2/{feature}/{action}` alongside v1
4. **Deprecate then remove** - mark as obsolete in one PR, remove in a follow-up

### Pre-Modification Checklist

1. Check [FILEMAP.md](FILEMAP.md) for impact
2. Search for all usages: `grep -r "InterfaceName\|MethodName" src/`
3. If the change affects the OpenAPI spec - frontend types are affected - regenerate and fix
4. If the change affects i18n keys - update all `.json` message files and all component usages
5. Document the breaking change in the commit body

## Local Development

```bash
dotnet run --project src/backend/MyProject.AppHost
```

Aspire is the sole local development workflow. It starts all infrastructure as containers and launches the API and frontend dev server. The Aspire Dashboard URL appears in the console.

Behavioral config (log levels, rate limits, JWT lifetimes, CORS, seed users) lives in `appsettings.Development.json`. Infrastructure connection strings are injected by Aspire via environment variables.

| File | Purpose |
|---|---|
| `appsettings.json` | Base/production defaults (placeholder values) |
| `appsettings.Development.json` | Dev overrides (generous JWT, debug logging, seed users, permissive rate limits) |
| `deploy/envs/production-example/` | Production template - `cp -r` to `deploy/envs/production/` |
| `deploy/docker-compose.yml` | Base service definitions (production only) |
| `deploy/docker-compose.production.yml` | Production overlay |
