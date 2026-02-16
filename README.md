<div align="center">

# NETrock

**Full-stack .NET 10 + SvelteKit foundation. Auth, permissions, background jobs, admin panel — production-ready out of the box.**

Clean Architecture. Fully tested. Fully dockerized. API-first — use the included frontend or bring your own.

[![CI](https://github.com/fpindej/netrock/actions/workflows/ci.yml/badge.svg)](https://github.com/fpindej/netrock/actions/workflows/ci.yml)
[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![SvelteKit](https://img.shields.io/badge/SvelteKit-Svelte_5-FF3E00?logo=svelte&logoColor=white)](https://svelte.dev/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15-4169E1?logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Redis](https://img.shields.io/badge/Redis-7-DC382D?logo=redis&logoColor=white)](https://redis.io/)
[![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?logo=docker&logoColor=white)](https://www.docker.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Discord](https://img.shields.io/badge/Discord-Join-5865F2?logo=discord&logoColor=white)](https://discord.gg/5rHquRptSh)

**[Live Demo](https://demo.netrock.dev)** · **[Documentation](#documentation)** · **[Quick Start](#quick-start)** · **[Discord](https://discord.gg/5rHquRptSh)**

</div>

---

## Why NETrock?

Every project starts the same way: authentication, role management, rate limiting, validation, API documentation, Docker setup... You spend weeks on infrastructure before writing a single line of business logic.

**NETrock skips all of that.** It ships a production-hardened .NET 10 API with a complete SvelteKit frontend — real security, real patterns, and real conventions that scale. Use the full stack as-is, or use just the API to power a React app, a mobile app, or anything that speaks HTTP.

Run the init script, pick a name, and start building your product.

### How is this different?

Most project templates give you a folder structure and stop there. They show you the architecture as a diagram, drop in a `Todo` entity, and call it a day. You still have to build authentication, wire up permissions, create the admin panel, configure Docker, write the CI pipeline, and handle all the edge cases — which is the actual work.

**NETrock ships working infrastructure.** Login works. Token rotation works. The permission system enforces role hierarchy. The admin panel manages users, roles, and background jobs. The OpenAPI spec documents every endpoint for any client to consume. The Docker stack spins up with health checks. CI runs your tests. All of it is tested and documented.

Other approaches come with different trade-offs:

- **Full-blown frameworks** give you everything out of the box, but you're locked into their abstractions forever. Every version upgrade is a migration project. You depend on the framework at runtime, and when it doesn't do what you need, you fight it.
- **Minimal starters** get you running fast, but they skip the hard parts. You'll spend the next month building what they left out — and you'll build it worse than if you'd had a reference implementation.
- **Building from scratch** means total control, but also total time investment. The patterns NETrock implements (token rotation with reuse detection, security stamp propagation, soft refresh, role hierarchy enforcement) take weeks to get right.

NETrock takes a different path: **fork it, init it, own it.** After initialization, there is no dependency on "the template." It's your code, your architecture, your product. Every decision is documented so you can understand it, change it, or throw it away. You get the benefit of a well-built foundation without any of the lock-in.

NETrock ships full-stack: a .NET 10 API and a complete SvelteKit frontend with admin panel, auth flows, dark mode, i18n, and more. Use both together and you have a working product from day one. But the API is designed to stand alone — every endpoint is documented via OpenAPI, so you can generate clients for React, Flutter, Swift, or anything else. The backend doesn't care what's consuming it.

---

## What You Get

### Backend — .NET 10 / C# 13

| Feature | Implementation |
|---|---|
| **Clean Architecture** | Domain → Application → Infrastructure → WebApi, with [architecture tests](src/backend/tests/MyProject.Architecture.Tests) enforcing dependency rules |
| **Authentication** | JWT in HttpOnly cookies, refresh token rotation with reuse detection, security stamp validation, remember-me persistent sessions |
| **Authorization** | Permission-based with custom roles — atomic permissions (`users.view`, `roles.manage`, …) assigned per role, enforced via `[RequirePermission]` attribute |
| **Role Hierarchy** | SuperAdmin > Admin > User — privilege escalation prevention, self-protection rules, system role guards |
| **Rate Limiting** | Global + per-endpoint policies (registration, auth, sensitive operations, admin mutations), configurable fixed-window with IP and user partitioning |
| **Validation** | FluentValidation + Data Annotations, flowing constraints into OpenAPI spec and generated TypeScript types |
| **Caching** | Redis (distributed) with auto-invalidation via EF Core interceptor, cache-aside pattern, key management |
| **Database** | PostgreSQL + EF Core with soft delete, full audit trail (created/updated/deleted by + at), global query filters |
| **API Documentation** | OpenAPI spec + Scalar UI, with custom transformers for enums, nullable types, numeric schemas, and camelCase query params |
| **Error Handling** | Result pattern for business logic, `ProblemDetails` ([RFC 9457](https://www.rfc-editor.org/rfc/rfc9457)) everywhere, structured error messages |
| **Logging** | Serilog → Seq with structured request logging and correlation |
| **Account Management** | Registration with CAPTCHA, login/logout, remember me, email verification, password reset, profile updates, account deletion |
| **Admin Panel API** | User CRUD with search and pagination, custom role management with permission editor, role assignment with hierarchy enforcement |
| **Background Jobs** | Hangfire with PostgreSQL persistence — recurring jobs via `IRecurringJobDefinition`, fire-and-forget, admin UI with trigger/pause/resume/restore, persistent pause state |
| **Email** | Pluggable email service (NoOp for dev — swap in your SMTP/SendGrid/etc.), templated emails for verification and password reset |
| **Health Checks** | `/health` (all), `/health/ready` (DB + Redis), `/health/live` (liveness) — Docker healthcheck integration |
| **Search** | User lookup with search and pagination in admin panel, PostgreSQL trigram similarity function pre-registered for custom use |
| **Testing** | 4 test projects — unit, component (mocked services), API integration (WebApplicationFactory), architecture enforcement |

### Frontend — SvelteKit / Svelte 5

| Feature | Implementation |
|---|---|
| **Svelte 5 Runes** | Modern reactivity with `$state`, `$derived`, `$effect` — no legacy stores or `export let` |
| **Type-Safe API Client** | Generated from OpenAPI spec via `openapi-typescript` — backend changes break the build, not the user |
| **Automatic Token Refresh** | 401 → refresh → retry, transparent to components, thundering-herd protection |
| **Tailwind CSS 4** | Utility-first styling with shadcn-svelte (bits-ui) headless components, CSS variable theming |
| **BFF Architecture** | Server-side API proxy handles cookies, CSRF validation, header filtering, and `X-Forwarded-For` propagation |
| **i18n** | Paraglide JS — type-safe keys, compile-time validation, SSR-compatible, auto-detection via Accept-Language |
| **Security Headers** | CSP with nonce mode, HSTS, X-Frame-Options, Referrer-Policy, Permissions-Policy on every response |
| **Permission Guards** | Layout-level + page-level route guards, per-permission nav item filtering, component-level conditional rendering |
| **Dark Mode** | Light/dark/system theme with localStorage persistence, FOUC prevention, and CSS variable theming |
| **Responsive Design** | Mobile-first with sidebar drawer, breakpoint-aware layouts, logical CSS properties (RTL-ready) |
| **Keyboard Shortcuts** | Global shortcuts (Cmd/Ctrl combos), platform-aware display, help dialog |
| **Error Handling** | Unified mutation error handler — rate limiting with cooldown timers, field-level validation with shake animations, generic errors with toast |
| **Admin UI** | User table with search/pagination, role card grid, permission checkbox editor, job dashboard with execution history |
| **Login UX** | API health indicator, form draft persistence (registration), animated success transition, CAPTCHA integration |

### Infrastructure & DevOps

| Feature | Implementation |
|---|---|
| **Fully Dockerized** | One `docker compose up` for 5 services — API, frontend (hot-reload), DB, Redis, Seq |
| **Init Script** | Interactive project bootstrapping — renames solution, configures ports, generates secrets, creates migration, starts Docker |
| **Deploy Script** | Multi-registry support (Docker Hub, GHCR, ACR, ECR, DigitalOcean), semantic versioning, platform selection |
| **CI Pipeline** | GitHub Actions with smart path filtering — backend-only PRs skip frontend checks and vice versa |
| **Docker Validation** | CI validates image builds on Dockerfile/dependency changes, with layer caching |
| **Dependabot** | Weekly NuGet, npm, and GitHub Actions updates with grouped minor+patch PRs |
| **Environment Config** | `.env` overrides for everything, documented precedence, working dev defaults out of the box |
| **Production Hardening** | Dev config stripping from production builds, reverse proxy trust configuration, CORS production safeguard |

---

## Security — Not an Afterthought

NETrock is built **security-first**. Every decision defaults to the most restrictive option, then selectively opens what's needed.

### Authentication & Session Security
- **JWT in HttpOnly cookies** — tokens never touch JavaScript, immune to XSS theft
- **Refresh token rotation** — single-use tokens with automatic family revocation on reuse detection (stolen token → all sessions invalidated)
- **Security stamp validation** — permission changes propagate to active sessions via SHA-256 hashed stamps in JWT claims, cached in Redis for performance
- **Soft refresh** — role/permission changes invalidate access tokens but preserve refresh tokens, so users silently re-authenticate instead of getting force-logged-out
- **Remember me** — persistent refresh tokens with configurable expiry, non-persistent sessions cleared on browser close

### Authorization & Access Control
- **Permission-based authorization** — atomic permissions (`users.view`, `users.manage`, `roles.manage`, …) enforced on every endpoint via `[RequirePermission]`
- **Role hierarchy protection** — SuperAdmin > Admin > User, with privilege escalation prevention (can't assign roles at or above your own rank)
- **Self-protection rules** — can't lock your own account, can't delete yourself, can't remove your own roles
- **System role guards** — SuperAdmin/Admin/User cannot be deleted or renamed, SuperAdmin permissions are implicit (never stored in DB)
- **Frontend mirrors backend** — route guards, nav filtering, and conditional rendering use the same permission claims, but the backend is always authoritative

### Transport & Headers
- **CORS production safeguard** — startup guard rejects `AllowAllOrigins` in non-development environments
- **CSP with nonce mode** — script-src locked down, Turnstile CAPTCHA whitelisted explicitly
- **Security headers on every response** — `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy`, `Permissions-Policy`, HSTS in production
- **CSRF protection** — Origin header validation in the SvelteKit API proxy for all state-changing requests

### Rate Limiting & Input Validation
- **Rate limiting** — global + per-endpoint policies (registration has stricter limits), configurable per environment, with IP and user partitioning
- **Input validation everywhere** — FluentValidation on backend (even if frontend already validates), Data Annotations flowing into OpenAPI spec
- **URL validation** — blocks `javascript:`, `file://`, `ftp://` schemes in user-supplied URLs

### Data Protection
- **Soft delete** — nothing is ever truly gone, every mutation tracked with who/when audit fields
- **Audit trail** — automatic `CreatedAt/By`, `UpdatedAt/By`, `DeletedAt/By` on every entity via EF Core interceptor
- **Dev config stripping** — `appsettings.Development.json` and `appsettings.Testing.json` excluded from production Docker images

---

## Quick Start

> **Want to see it first?** Check out the [live demo](https://demo.netrock.dev).

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 22+](https://nodejs.org/)
- [Git](https://git-scm.com/)

### 1. Clone & Initialize

```bash
git clone https://github.com/fpindej/netrock.git my-saas
cd my-saas
```

**macOS / Linux:**
```bash
chmod +x init.sh
./init.sh
```

**Windows (PowerShell):**
```powershell
.\init.ps1
```

The init script will:
1. Ask for your **project name** (e.g., `Acme`)
2. Ask for a **base port** (default `13000`)
3. Rename all files, directories, namespaces, and configs
4. Generate a random JWT secret
5. Optionally create the initial EF Core migration
6. Optionally start Docker services

### 2. Launch Everything

```bash
docker compose -f docker-compose.local.yml up -d --build
```

That's it. Your entire stack is running:

| Service | URL |
|---|---|
| **Frontend** | `http://localhost:<BASE_PORT>` |
| **API Docs (Scalar)** | `http://localhost:<BASE_PORT + 2>/scalar/v1` |
| **Hangfire Dashboard** | `http://localhost:<BASE_PORT + 2>/hangfire` |
| **Seq (Structured Logs)** | `http://localhost:<BASE_PORT + 8>` |

Three test users are seeded in development:

| Role | Email | Password |
|---|---|---|
| SuperAdmin | `superadmin@test.com` | `SuperAdmin123` |
| Admin | `admin@test.com` | `Admin123` |
| User | `user@test.com` | `User123` |

### 3. Start Building

The foundation is in place. Add your domain entities, services, and pages — the architecture guides you:

```
# Add a backend feature
src/backend/YourProject.Domain/Entities/         → Entity
src/backend/YourProject.Application/Features/    → Interface + DTOs
src/backend/YourProject.Infrastructure/Features/ → Implementation
src/backend/YourProject.WebApi/Features/         → Controller + Validation

# Add a frontend page
src/frontend/src/routes/(app)/your-feature/      → Page + components
src/frontend/src/lib/api/                        → Auto-generated types
```

---

## Architecture

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
    ├── PostgreSQL (:5432)  — EF Core, soft delete, audit trails, Hangfire storage
    ├── Redis (:6379)       — Distributed cache, security stamp lookup
    ├── Hangfire            — Recurring + fire-and-forget background jobs
    └── Seq (:80)           — Structured log aggregation
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

All tests run in-process — no Docker, PostgreSQL, or Redis required:

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
│   ├── YourProject.Infrastructure/   # EF Core, Identity, Redis, Hangfire, email, implementations
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

---

## Developer Workflows

### Frontend dev — tweak backend config without touching code

Edit `.env`, restart Docker:

```bash
# Longer JWT tokens, relaxed rate limit
Authentication__Jwt__ExpiresInMinutes=300
RateLimiting__Global__PermitLimit=1000
```

```bash
docker compose -f docker-compose.local.yml up -d
```

### Backend dev — debug with breakpoints in Rider/VS

1. Stop the API container: `docker compose -f docker-compose.local.yml stop api`
2. Set `API_URL=http://host.docker.internal:5142` in `.env`
3. Restart frontend: `docker compose -f docker-compose.local.yml restart frontend`
4. Launch API from your IDE — breakpoints work, frontend proxies to it

### Database migrations

```bash
dotnet ef migrations add <Name> \
  --project src/backend/<YourProject>.Infrastructure \
  --startup-project src/backend/<YourProject>.WebApi \
  --output-dir Features/Postgres/Migrations
```

Migrations auto-apply on startup in Development.

### Deployment

Build and push Docker images with semantic versioning:

```bash
./deploy.sh backend --minor    # Build, bump minor version, push to registry
./deploy.sh frontend --patch   # Same for frontend
```

Supports Docker Hub, GitHub Container Registry, Azure ACR, AWS ECR, DigitalOcean, and custom registries.

---

## Documentation

NETrock includes **4,500+ lines** of structured documentation designed for both humans and AI coding assistants. Whether you use Claude Code, GitHub Copilot, Cursor, or any other AI tool, the documentation helps the AI understand your codebase and follow your conventions.

| File | Purpose |
|---|---|
| [`CLAUDE.md`](CLAUDE.md) | Hard rules, pre-commit checks, architecture overview |
| [`AGENTS.md`](AGENTS.md) | Full developer guide — security, git discipline, error handling, local dev |
| [`src/backend/AGENTS.md`](src/backend/AGENTS.md) | Backend conventions — entities, Result pattern, EF Core, controllers, testing |
| [`src/frontend/AGENTS.md`](src/frontend/AGENTS.md) | Frontend conventions — routing, API client, components, styling, i18n |
| [`SKILLS.md`](SKILLS.md) | Step-by-step recipes for 25+ common operations |
| [`FILEMAP.md`](FILEMAP.md) | Change impact tables — "when you change X, also update Y" |

The documentation includes:
- **Mechanical recipes** — follow step-by-step to add entities, endpoints, pages, permissions, background jobs
- **Change impact tracking** — know exactly what downstream files to update when you change something
- **Pattern enforcement** — conventions are documented precisely enough for AI agents to follow them

---

## Localization

Production-ready i18n with [Paraglide JS](https://inlang.com/m/gerre34r/library-inlang-paraglideJs):

- **Type-safe keys** — generated from `en.json`, compile-time errors on missing keys
- **SSR-compatible** — correct `lang` attribute on first render, no hydration mismatch
- **Auto-detection** — cookie preference → `Accept-Language` header fallback
- **Adding a language** — create `es.json`, register in `i18n.ts`, done

Ships with English and Czech. Adding a new language is a single JSON file.

---

## What This Is NOT

NETrock is opinionated by design. It's not:

- **A generic starter** — it makes real choices (PostgreSQL, not "any database"; JWT cookies, not "pluggable auth")
- **A microservices framework** — it's a monolith, because that's what 95% of products should start as
- **A frontend framework** — SvelteKit is a full, production-ready frontend, but you can also use just the API with any other frontend
- **Magic** — you still need to understand .NET (and SvelteKit if you keep it). NETrock gives you the architecture, not the knowledge

---

## Before You Ship

NETrock works out of the box for local development, but there are things you need to configure before going to production. This checklist covers what the template **can't decide for you**.

### Must Do

- [ ] **Email service** — replace `NoOpEmailService` with a real provider (SMTP, SendGrid, Postmark, etc.). The NoOp service just logs emails to Seq. Configure via `Email__Smtp__*` env vars or swap the service registration in `ServiceCollectionExtensions.cs`
- [ ] **CORS origins** — set `Cors__AllowedOrigins` to your production domain(s). The app **will refuse to start** if `AllowAllOrigins` is `true` outside of Development — this is intentional
- [ ] **JWT secret** — the init script generates one, but verify it's set in production via `Authentication__Jwt__Key` (64+ chars, cryptographically random)
- [ ] **Database** — point `ConnectionStrings__Database` to your production PostgreSQL instance
- [ ] **CAPTCHA keys** — replace the Cloudflare Turnstile development keys with production keys (`Captcha__SecretKey` backend, `PUBLIC_TURNSTILE_SITE_KEY` frontend)
- [ ] **Frontend URL in emails** — set `Email__FrontendBaseUrl` to your production domain so email verification and password reset links work

### Should Do

- [ ] **Redis** — enable for production (`Caching__Redis__Enabled=true`) with real credentials. Without it, the app falls back to in-memory cache (fine for single-instance, not for scaling)
- [ ] **Reverse proxy** — if behind nginx/load balancer, configure `Hosting__ReverseProxy__TrustedNetworks` and `TrustedProxies` so rate limiting uses real client IPs
- [ ] **Logging** — replace Seq with your production logging solution or point Serilog at your provider. Adjust log levels (`Serilog__MinimumLevel__Default=Information`)
- [ ] **Rate limits** — review the production defaults in `appsettings.json` and adjust for your expected traffic
- [ ] **Backups** — set up automated PostgreSQL backups. NETrock uses soft delete, but that doesn't replace real backups
- [ ] **Monitoring** — the health check endpoints (`/health`, `/health/ready`, `/health/live`) are ready for your uptime monitoring

### Good to Know

- **Hangfire dashboard** is automatically disabled in production. Use the admin API endpoints (`/api/admin/jobs/*`) instead
- **HTTPS** is forced in production via `Hosting__ForceHttps=true` (default). Development runs on HTTP
- **Dev config is stripped** from production Docker images — `appsettings.Development.json` and `appsettings.Testing.json` are removed at build time
- **CORS startup guard** will crash the app on purpose if you deploy with `AllowAllOrigins=true` — this is a security feature, not a bug

---

## Contributing

Contributions are welcome! See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

---

## Support the Project

NETrock is free and open source under the [MIT License](LICENSE). If it saves you time, consider supporting its development:

<a href="https://buymeacoffee.com/fpindej" target="_blank"><img src="https://cdn.buymeacoffee.com/buttons/v2/default-yellow.png" alt="Buy Me A Coffee" height="50"></a>

- Star the repo on [GitHub](https://github.com/fpindej/netrock)
- Join the [Discord community](https://discord.gg/5rHquRptSh)

### Need help building your product?

NETrock was built by a developer who ships production software. If you need:

- **Custom development** — web apps, APIs, dashboards, mobile backends, built on NETrock's foundation
- **Architecture consulting** — code review, scaling strategy, security hardening for your .NET project
- **Team training** — hands-on workshops on the patterns and conventions used here

Reach out — [contact@mail.pindej.cz](mailto:contact@mail.pindej.cz)

---

## License

This project is licensed under the [MIT License](LICENSE).
