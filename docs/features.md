# Features

> Back to [README](../README.md)

## Backend — .NET 10 / C# 13

| Feature | Implementation |
|---|---|
| **Clean Architecture** | Domain → Application → Infrastructure → WebApi, with [architecture tests](../src/backend/tests/MyProject.Architecture.Tests) enforcing dependency rules |
| **Authentication** | JWT in HttpOnly cookies, refresh token rotation with reuse detection, security stamp validation, remember-me persistent sessions |
| **Authorization** | Permission-based with custom roles — atomic permissions (`users.view`, `roles.manage`, …) assigned per role, enforced via `[RequirePermission]` attribute |
| **Role Hierarchy** | SuperAdmin > Admin > User — privilege escalation prevention, self-protection rules, system role guards |
| **Rate Limiting** | Global + per-endpoint policies (registration, auth, sensitive operations, admin mutations), configurable fixed-window with IP and user partitioning |
| **Validation** | FluentValidation + Data Annotations, flowing constraints into OpenAPI spec and generated TypeScript types |
| **Caching** | HybridCache (in-process L1) with auto-invalidation via EF Core interceptor, stampede protection, key management |
| **Database** | PostgreSQL + EF Core with soft delete, full audit trail (created/updated/deleted by + at), global query filters |
| **API Documentation** | OpenAPI spec + Scalar UI, with custom transformers for enums, nullable types, numeric schemas, and camelCase query params |
| **Error Handling** | Result pattern for business logic, `ProblemDetails` ([RFC 9457](https://www.rfc-editor.org/rfc/rfc9457)) everywhere, structured error messages |
| **Logging** | Serilog → OpenTelemetry with structured request logging, correlation, and Aspire Dashboard |
| **Account Management** | Registration with CAPTCHA, login/logout, remember me, email verification, password reset, profile updates, account deletion |
| **Admin Panel API** | User CRUD with search and pagination, custom role management with permission editor, role assignment with hierarchy enforcement |
| **Background Jobs** | Hangfire with PostgreSQL persistence — recurring jobs via `IRecurringJobDefinition`, fire-and-forget, admin UI with trigger/pause/resume/restore, persistent pause state |
| **Email** | Pluggable email service (NoOp for dev — swap in your SMTP/SendGrid/etc.), Fluid (Liquid) template engine with base layout, templated emails for verification, password reset, admin-initiated reset, and invitation |
| **File Storage** | S3-compatible blob storage via generic `IFileStorageService` — MinIO locally, any S3 provider in production (AWS S3, Cloudflare R2, DigitalOcean Spaces, Backblaze B2). Avatar upload with SkiaSharp image processing (resize, WebP compression) |
| **Health Checks** | `/health` (all), `/health/ready` (DB + S3), `/health/live` (liveness) — Docker healthcheck integration |
| **Search** | User lookup with search and pagination in admin panel, PostgreSQL trigram similarity function pre-registered for custom use |
| **Testing** | 4 test projects — unit, component (mocked services), API integration (WebApplicationFactory), architecture enforcement |

## Frontend — SvelteKit / Svelte 5

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
| **Avatar Upload** | Drag-and-drop file upload with client-side validation, preview, SkiaSharp server-side compression to WebP, S3 storage |
| **Login UX** | API health indicator, form draft persistence (registration), animated success transition, CAPTCHA integration |

## Infrastructure & DevOps

| Feature | Implementation |
|---|---|
| **Aspire Local Dev** | One `dotnet run` for the full stack — API, frontend (hot-reload), PostgreSQL, MinIO (S3 storage), OpenTelemetry Dashboard |
| **Production Docker** | Docker Compose with production overlay — hardened containers, resource limits, network segmentation |
| **Init Script** | Interactive project bootstrapping — renames solution, configures ports, creates migration, launches Aspire |
| **Deploy Script** | Multi-registry support (Docker Hub, GHCR, ACR, ECR, DigitalOcean), semantic versioning, platform selection |
| **CI Pipeline** | GitHub Actions with smart path filtering — backend-only PRs skip frontend checks and vice versa |
| **Docker Validation** | CI validates image builds on Dockerfile/dependency changes, with layer caching |
| **Dependabot** | Weekly NuGet, npm, and GitHub Actions updates with grouped minor+patch PRs |
| **Environment Config** | `.env` overrides for everything, documented precedence, working dev defaults out of the box |
| **Production Hardening** | Dev config stripping from production builds, reverse proxy trust configuration, CORS production safeguard |
