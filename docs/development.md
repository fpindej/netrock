# Development

> Back to [README](../README.md)

## Local Development (Aspire)

Aspire is the sole local development workflow. It starts all infrastructure (PostgreSQL, MinIO, MailPit) as containers and launches the API and frontend dev server.

```bash
dotnet run --project src/backend/MyProject.AppHost
```

The Aspire Dashboard URL appears in the console. All service URLs (API docs, pgAdmin, MinIO, MailPit) are linked from the Dashboard.

> **Note:** The init scripts launch Aspire with `DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true` so the dashboard opens without a login token. When running `dotnet run` manually, you'll need the token from the console output (or set the same variable).

### Debugging with breakpoints in Rider/VS

Launch the AppHost project from your IDE. The API runs in-process with full debugger support. Infrastructure containers are still managed by Aspire.

### Configuration

Behavioral config (log levels, rate limits, JWT lifetimes, CORS, seed users, OAuth providers) lives in `appsettings.Development.json`. Infrastructure connection strings are injected by Aspire via environment variables - no manual config needed.

### Email Testing

MailPit captures all outgoing emails locally. Access the MailPit web UI from the Aspire Dashboard (port `BASE_PORT + 8`). Email verification, password reset, invitation, and 2FA disable notification emails all appear there immediately.

---

## Claude Code Skills

NETrock ships with 20+ native Claude Code skills that automate common development tasks. Type `/` in Claude Code to see all available skills.

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

The project context files (`CLAUDE.md`, `FILEMAP.md`) plus specialized agents, convention skills, and lifecycle hooks provide Claude Code with deep understanding of the architecture and conventions. No separate onboarding needed.

---

## MCP Server (Claude Code integration)

The API embeds an MCP (Model Context Protocol) server at `/mcp`, enabled in Development only. When the API is running (via Aspire), Claude Code can use it to query the database, inspect schema, check health, list users, and manage background jobs - all through the running application's DI container.

The `.mcp.json` at the project root configures Claude Code to connect automatically. Tools available:

| Tool | Description |
|---|---|
| `get-health` | Health status of all dependencies (DB, S3) |
| `query-database` | Execute read-only SQL (SELECT only, 100-row limit) |
| `get-schema` | Database schema from EF Core model |
| `list-users` | Paginated user list with search |
| `list-jobs` | All recurring background jobs |
| `trigger-job` | Trigger immediate job execution |

The MCP endpoint is never exposed in production - gated by `IsDevelopment()`.

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

Production deployment is up to you. The project provides production-ready Dockerfiles (`src/backend/MyProject.WebApi/Dockerfile` and `src/frontend/Dockerfile`) but does not prescribe an orchestrator, build pipeline, or registry. Use whatever fits your infrastructure.

See [Before You Ship](before-you-ship.md) for the configuration checklist.
