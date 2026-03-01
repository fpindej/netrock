<div align="center">

# NETrock

**Full-stack .NET 10 + SvelteKit foundation. Auth, permissions, background jobs, admin panel - wired up so you can skip the boilerplate.**

Clean Architecture. 1000+ tests. Dockerized. API-first - use the included frontend or bring your own.

[![CI](https://github.com/fpindej/netrock/actions/workflows/ci.yml/badge.svg)](https://github.com/fpindej/netrock/actions/workflows/ci.yml)
[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![SvelteKit](https://img.shields.io/badge/SvelteKit-Svelte_5-FF3E00?logo=svelte&logoColor=white)](https://svelte.dev/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15-4169E1?logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?logo=docker&logoColor=white)](https://www.docker.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Discord](https://img.shields.io/badge/Discord-Join-5865F2?logo=discord&logoColor=white)](https://discord.gg/5rHquRptSh)
[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/fpindej/netrock)

**[Live Demo](https://demo.netrock.dev)** · **[Documentation](#documentation)** · **[Quick Start](#quick-start)** · **[Discord](https://discord.gg/5rHquRptSh)**

</div>

---

## Why NETrock?

Every project starts the same way: authentication, role management, rate limiting, validation, API documentation, Docker setup... You spend weeks on infrastructure before writing a single line of business logic.

**NETrock skips all of that.** It ships a .NET 10 API with a SvelteKit frontend - auth that actually works, documented conventions, and the kind of infrastructure you'd build anyway. Login works. Token rotation works. The permission system enforces role hierarchy. The admin panel manages users, roles, and background jobs. The Docker stack spins up with health checks. CI runs your tests.

**Fork it, init it, own it.** After initialization, there is no dependency on "the template." It's your code, your architecture, your product. Every decision is documented so you can understand it, change it, or throw it away.

---

## What You Get

**Backend** - JWT auth with token rotation and reuse detection, TOTP two-factor authentication, permission-based authorization with role hierarchy, transactional email delivery (MailKit), rate limiting, HybridCache with stampede protection, PostgreSQL with soft delete and audit trails, S3-compatible file storage (MinIO locally, any S3 provider in production), Hangfire background jobs, OpenAPI docs, health checks, Result pattern with ProblemDetails everywhere. [See full details ->](docs/features.md#backend--net-10--c-13)

**Frontend** - Svelte 5 runes, type-safe API client generated from OpenAPI, automatic token refresh, Tailwind CSS 4 with shadcn-svelte, BFF proxy with CSRF protection, i18n, security headers, permission guards, dark mode, admin panel with user/role/job management. [See full details ->](docs/features.md#frontend--sveltekit--svelte-5)

**Infrastructure** - Aspire AppHost for local development (one command, full OTEL dashboard, MailPit for local email testing), structured logs, metrics, and traces via OpenTelemetry, Docker Compose for production deployment, init script for project bootstrapping, build script with multi-registry support, GitHub Actions CI with smart path filtering, Dependabot. [See full details ->](docs/features.md#infrastructure--devops)

**Security** - Security-first design with HttpOnly JWT cookies, refresh token rotation with reuse detection, TOTP two-factor authentication with challenge tokens and recovery codes, security stamp propagation, CSP with nonces, CORS startup guard, rate limiting, and input validation everywhere. [See full details ->](docs/security.md)

---

## Quick Start

> **Want to see it first?** Check out the [live demo](https://demo.netrock.dev).

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 22+](https://nodejs.org/) (run `corepack enable` for pnpm)
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

The init script will ask for your project name and base port, then rename everything and optionally create the initial migration.

### 2. Launch Everything

```bash
dotnet run --project src/backend/MyProject.AppHost
```

That's it. Aspire starts all infrastructure (PostgreSQL, MinIO) as containers and launches the API and frontend dev server. The Aspire Dashboard URL appears in the console — all service URLs (API docs, pgAdmin, MinIO) are linked from the Dashboard.

| Service | URL |
|---|---|
| **Aspire Dashboard** | Shown in console output |
| **Frontend** | `http://localhost:<BASE_PORT>` (default: `http://localhost:13000`) |
| **MailPit (Email Testing)** | `http://localhost:<BASE_PORT + 8>` |

Three test users are seeded (configured in `appsettings.Development.json`):

| Role | Email | Password |
|---|---|---|
| SuperAdmin | `superadmin@test.com` | `SuperAdmin123!` |
| Admin | `admin@test.com` | `AdminUser123!` |
| User | `testuser@test.com` | `TestUser123!` |

### 3. Start Building

Add your domain entities, services, and pages — the architecture guides you. See [SKILLS.md](SKILLS.md) for step-by-step recipes.

---

## Documentation

| File | Purpose |
|---|---|
| [`CLAUDE.md`](CLAUDE.md) | Hard rules, pre-commit checks, architecture overview |
| [`AGENTS.md`](AGENTS.md) | Full developer guide — security, git discipline, error handling, local dev |
| [`src/backend/AGENTS.md`](src/backend/AGENTS.md) | Backend conventions — entities, Result pattern, EF Core, controllers, testing |
| [`src/frontend/AGENTS.md`](src/frontend/AGENTS.md) | Frontend conventions — routing, API client, components, styling, i18n |
| [`SKILLS.md`](SKILLS.md) | Step-by-step recipes for 35+ common operations |
| [`FILEMAP.md`](FILEMAP.md) | Change impact tables — "when you change X, also update Y" |

Deep dives: **[Features](docs/features.md)** · **[Security](docs/security.md)** · **[Architecture](docs/architecture.md)** · **[Development](docs/development.md)** · **[Before You Ship](docs/before-you-ship.md)** · **[Troubleshooting](docs/troubleshooting.md)**

---

## Localization

i18n with [Paraglide JS](https://inlang.com/m/gerre34r/library-inlang-paraglideJs) — type-safe keys, SSR-compatible, auto-detection via `Accept-Language`. Ships with English and Czech. Adding a language is a single JSON file.

---

## What This Is NOT

NETrock is opinionated by design. It's not:

- **A generic starter** — it makes real choices (PostgreSQL, not "any database"; JWT cookies, not "pluggable auth")
- **A microservices framework** — it's a monolith, because that's what 95% of products should start as
- **A frontend framework** — SvelteKit is included, but you can use just the API with any other frontend
- **Magic** — you still need to understand .NET (and SvelteKit if you keep it)

---

## Contributing

Contributions are welcome! See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

---

## Support the Project

NETrock is free and open source under the [MIT License](LICENSE). If it saves you time, consider supporting its development:

<a href="https://buymeacoffee.com/fpindej" target="_blank"><img src="https://cdn.buymeacoffee.com/buttons/v2/default-yellow.png" alt="Buy Me A Coffee" height="50"></a>

Star the repo on [GitHub](https://github.com/fpindej/netrock) · Join the [Discord](https://discord.gg/5rHquRptSh) · Need custom development, consulting, or training? [Get in touch](mailto:contact@mail.pindej.cz)

---

## License

This project is licensed under the [MIT License](LICENSE).
