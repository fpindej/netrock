# Development

> Back to [README](../README.md)

## Local Development (Aspire)

Aspire is the sole local development workflow. It starts all infrastructure (PostgreSQL, MinIO, MailPit) as containers and launches the API and frontend dev server.

```bash
dotnet run --project src/backend/MyProject.AppHost
```

The Aspire Dashboard URL appears in the console. All service URLs (API docs, pgAdmin, MinIO, MailPit) are linked from the Dashboard.

### Debugging with breakpoints in Rider/VS

Launch the AppHost project from your IDE. The API runs in-process with full debugger support. Infrastructure containers are still managed by Aspire.

### Configuration

Behavioral config (log levels, rate limits, JWT lifetimes, CORS, seed users, OAuth providers) lives in `appsettings.Development.json`. Infrastructure connection strings are injected by Aspire via environment variables - no manual config needed.

### Email Testing

MailPit captures all outgoing emails locally. Access the MailPit web UI from the Aspire Dashboard (port `BASE_PORT + 8`). Email verification, password reset, invitation, and 2FA disable notification emails all appear there immediately.

---

## Claude Code Skills

NETrock ships with 20 native Claude Code skills that automate common development tasks. Type `/` in Claude Code to see all available skills.

Key skills for daily development:

| Skill | When to use |
|---|---|
| `/new-feature` | Adding a new full-stack feature (entity through to frontend page) |
| `/new-endpoint` | Adding an API endpoint to an existing feature |
| `/new-entity` | Creating a new backend entity with EF Core config |
| `/new-page` | Creating a new frontend page with routing and i18n |
| `/gen-types` | After changing backend DTOs or endpoints |
| `/create-pr` | When your work is ready for review |
| `/review-pr` | Reviewing someone else's PR |
| `/review-design` | Checking UI/UX quality of frontend components |

The project context files (`CLAUDE.md`, `AGENTS.md`, `FILEMAP.md`, backend/frontend `AGENTS.md`) provide Claude Code with deep understanding of the architecture and conventions. No separate onboarding needed.

---

## Database Migrations

```bash
dotnet ef migrations add <Name> \
  --project src/backend/<YourProject>.Infrastructure \
  --startup-project src/backend/<YourProject>.WebApi \
  --output-dir Persistence/Migrations
```

Migrations auto-apply on startup in Development.

---

## Production Deployment

Docker Compose is used for production only. Aspire is not involved.

```bash
./deploy/up.sh production up -d
```

See [Before You Ship](before-you-ship.md) for the full production checklist.

---

## Build & Push

Build and push Docker images with semantic versioning:

```bash
./deploy/build.sh backend --minor    # Build, bump minor version, push to registry
./deploy/build.sh frontend --patch   # Same for frontend
```

Supports Docker Hub, GitHub Container Registry, Azure ACR, AWS ECR, DigitalOcean, and custom registries.
