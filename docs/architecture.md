# Architecture

> Back to [README](../README.md)

```
Frontend (SvelteKit :5173)
    │
    │  /api/* proxy (catch-all server route)
    │  Forwards cookies + headers, validates CSRF origin
    ▼
Backend API (.NET :8080)
    │
    │  Clean Architecture
    │  WebApi → Application ← Infrastructure → Domain (+Shared)
    │
    ├── PostgreSQL           — EF Core, soft delete, audit trails, Hangfire storage
    ├── MinIO                — S3-compatible blob storage (avatars, file uploads)
    ├── Hangfire             — Recurring + fire-and-forget background jobs
    └── OpenTelemetry ──────→ Aspire Dashboard (local) / OTLP endpoint (production)
```

The backend follows Clean Architecture with **architecture tests** that enforce dependency direction at build time — Domain and Shared have zero dependencies, Application only references Domain and Shared, Infrastructure never references WebApi. Breaking these rules fails the build.

---

## Testing

NETrock is thoroughly tested across 4 test projects, covering every layer of the backend:

| Project | What it covers |
|---|---|
| **Unit Tests** | Result pattern, error messages, phone normalization, base entity, roles, permissions |
| **Component Tests** | Auth service (login, register, refresh, token rotation), admin service (hierarchy, role assignment, lock/delete), role management, user service |
| **API Tests** | Full HTTP pipeline (status codes, auth gates, ProblemDetails shape), all validators, response contract testing, permission enforcement, rate limiting |
| **Architecture Tests** | Layer dependency direction, naming conventions, access modifiers |

All tests run in-process — no Docker or PostgreSQL required:

```bash
dotnet test src/backend/MyProject.slnx -c Release
```

---

## Project Structure

```
src/
├── backend/                          # .NET 10 API (Clean Architecture)
│   ├── YourProject.Shared/           # Result pattern, error types, cross-cutting utilities
│   ├── YourProject.Domain/           # Entities with audit fields and soft delete
│   ├── YourProject.Application/      # Interfaces, DTOs, service contracts, permissions
│   ├── YourProject.Infrastructure/   # EF Core, Identity, HybridCache, Hangfire, S3 storage, email, implementations
│   ├── YourProject.WebApi/           # Controllers, middleware, validation, authorization
│   └── tests/
│       ├── YourProject.Unit.Tests/        # Pure logic tests (Result, entities, roles, permissions)
│       ├── YourProject.Component.Tests/   # Service tests with mocked dependencies
│       ├── YourProject.Api.Tests/         # HTTP integration tests + validator tests
│       └── YourProject.Architecture.Tests/ # Dependency direction + naming enforcement
│
└── frontend/                         # SvelteKit frontend
    └── src/
        ├── lib/
        │   ├── api/                  # Type-safe API client + generated OpenAPI types
        │   ├── components/           # Feature-organized with barrel exports
        │   │   ├── admin/            # Admin components (tables, cards, editors)
        │   │   ├── auth/             # Login, register, CAPTCHA, password reset
        │   │   ├── layout/           # Sidebar, header, theme, language, shortcuts
        │   │   ├── profile/          # Profile editing, avatar management
        │   │   ├── settings/         # Password change, account deletion
        │   │   └── ui/               # shadcn-svelte (button, card, dialog, input, ...)
        │   ├── state/                # Reactive state (.svelte.ts) — theme, cooldown, shake, sidebar, shortcuts
        │   └── utils/                # Permissions, platform detection, class merging
        ├── routes/
        │   ├── (app)/                # Authenticated pages with sidebar layout
        │   │   ├── admin/            # User management, role management, job dashboard
        │   │   ├── profile/          # User profile
        │   │   └── settings/         # Account settings
        │   ├── (public)/             # Login, forgot/reset password, email verification
        │   └── api/                  # CSRF-protected API proxy to backend
        └── messages/                 # i18n translations (en, cs)
```
