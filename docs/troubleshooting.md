# Troubleshooting & FAQ

> Back to [README](../README.md) · Related: [Development](development.md) · [Security](security.md) · [Architecture](architecture.md)

## Contents

- [Quick Diagnostics](#quick-diagnostics)
- [Prerequisites & Versions](#prerequisites--versions)
- [Init Script](#init-script)
- [Docker & Compose](#docker--compose)
- [Backend Development](#backend-development)
- [Frontend Development](#frontend-development)
- [Authentication & Authorization](#authentication--authorization)
- [CI/CD](#cicd)
- [Production Deployment](#production-deployment)
- [General FAQ](#general-faq)
- [Still Stuck?](#still-stuck)

---

## Quick Diagnostics

Run these commands to quickly assess the state of your environment:

```bash
# Are services running?
./deploy/up.sh local ps                        # Linux / macOS
.\deploy\up.ps1 local ps                       # Windows PowerShell

# Recent logs from a specific service
./deploy/up.sh local logs --tail 30 api

# API health
curl http://localhost:<API_PORT>/health/live

# Toolchain versions
dotnet --version
node --version
pnpm --version
docker compose version
```

---

## Prerequisites & Versions

| Tool | Required | Check |
|---|---|---|
| .NET SDK | 10.0+ (pinned in `global.json`) | `dotnet --version` |
| Node.js | 22+ | `node --version` |
| pnpm | 10.x (managed via corepack) | `pnpm --version` |
| Docker | Recent with Compose V2 | `docker compose version` |
| Git | Any recent version | `git --version` |

If pnpm is not found, enable corepack first:

```bash
corepack enable
pnpm --version
```

---

## Init Script

### Permission denied running `init.sh`

**Cause:** The script doesn't have execute permission.

**Fix:**

```bash
chmod +x init.sh
./init.sh
```

### PowerShell execution policy blocks `init.ps1` (Windows)

**Cause:** Windows defaults to a `Restricted` execution policy that prevents running `.ps1` scripts.

**Fix:** Run one of:

```powershell
# Option 1: Bypass for this session only
Set-ExecutionPolicy -Scope Process Bypass
.\init.ps1

# Option 2: Bypass inline
pwsh -ExecutionPolicy Bypass -File init.ps1
```

### Name validation fails (`must be PascalCase`)

**Cause:** The project name must match `^[A-Z][a-zA-Z0-9]*$` — start with an uppercase letter, alphanumeric only. No hyphens, underscores, or spaces.

**Valid:** `MyAwesomeApi`, `TodoApp`, `WebApi`
**Invalid:** `my-app`, `todo_app`, `123App`

### Port conflict during init (`EADDRINUSE` / `address already in use`)

**Cause:** The base port (default `13000`) or one of its offsets is already in use. The template allocates 7 ports from your base:

| Offset | Service |
|---|---|
| +0 | Frontend |
| +2 | API |
| +4 | PostgreSQL |
| +8 | Seq |
| +10 | MinIO |
| +12 | MinIO Console |

**Fix:** Check what's using the port and pick a different base:

```bash
lsof -i :13000           # macOS / Linux
netstat -ano | findstr :13000   # Windows
```

Choose a base port between 1024 and 65530 that leaves room for all offsets.

### `dotnet-ef` not found

**Cause:** The EF Core CLI tool isn't installed or restored.

**Fix:**

```bash
dotnet tool restore
```

This restores tools declared in `.config/dotnet-tools.json`, including `dotnet-ef`.

### Init succeeds but Docker fails to start

**Cause:** Missing prerequisites or Docker daemon not running.

**Fix:** Verify all prerequisites:

```bash
git --version
dotnet --version
docker info          # Must succeed — if not, start Docker Desktop
node --version
pnpm --version       # If missing: corepack enable
```

---

## Docker & Compose

> `./deploy/up.sh` (Linux/macOS) and `.\deploy\up.ps1` (Windows) are thin wrappers around `docker compose`. All standard subcommands work: `up`, `down`, `logs`, `ps`, `restart`, `exec`, etc.

### Container keeps restarting / exiting with non-zero code

**Cause:** A service is crashing on startup. Check the logs first — the error message tells you what to do next.

**Fix:**

```bash
./deploy/up.sh local logs api
./deploy/up.sh local logs frontend
```

Common log patterns and what they mean:

| Log pattern | Likely cause | See section |
|---|---|---|
| `Connection refused` / `could not connect to server` | Database not ready yet | [Database health check failing](#database-health-check-failing) |
| `Value cannot be null` / `'JWT_SECRET_KEY'` | Missing environment variable | Check `deploy/envs/local/compose.env` |
| `Address already in use` / `EADDRINUSE` | Port conflict | [Port already in use](#port-already-in-use-eaddrinuse) |
| `no such file or directory` | Volume mount issue or missing config | Verify `docker-compose.local.yml` mounts |

### Database health check failing (`pg_isready`)

**Cause:** PostgreSQL can be slow to initialize, especially on first run. The health check has a `start_period` of 15s, retries every 5s, with 5 retries — it can take up to 40 seconds before the database is reported healthy.

**Fix:** Wait 30–60 seconds on first startup. If it still fails:

```bash
./deploy/up.sh local logs db
```

Look for disk space issues, permission errors, or invalid `POSTGRES_USER` / `POSTGRES_PASSWORD` in `deploy/envs/local/compose.env`.

### Frontend can't reach the API (`fetch failed` / `ECONNREFUSED`)

**Cause:** The frontend connects to the API via Docker's internal network using `API_URL` (default: `http://api:8080`). The `api` service bridges both the `frontend` and `backend` networks; the `frontend` service is on the `frontend` network only.

**Fix:**

1. Verify the API is running and healthy:
   ```bash
   ./deploy/up.sh local ps
   ```
2. Check `API_URL` in `deploy/envs/local/compose.env` — it should be `http://api:8080` for Docker
3. If running the API outside Docker (from your IDE), set `API_URL=http://host.docker.internal:5142` in `compose.env` and restart the frontend container. Port 5142 is the default from `launchSettings.json`. See [Development — Backend dev](development.md#backend-dev--debug-with-breakpoints-in-ridervs) for the full workflow.

### Port already in use (`EADDRINUSE`)

**Cause:** Another Docker stack or host service is bound to the same port.

**Fix:**

```bash
lsof -i :<port>              # macOS / Linux
netstat -ano | findstr :<port>    # Windows
```

Either stop the conflicting process or change the port mapping in `deploy/envs/local/compose.env`.

### `node_modules` volume is stale

**Cause:** The local Docker setup uses a named volume (`frontend_node_modules`) for `/app/node_modules` to prevent the host bind mount from overwriting container dependencies. After dependency changes in `package.json`, this volume can become outdated.

**Fix:**

```bash
./deploy/up.sh local down
docker volume rm $(docker volume ls -q | grep frontend_node_modules)
./deploy/up.sh local up -d --build
```

### MinIO bucket not created

**Cause:** The storage service didn't initialize properly, or the bucket name doesn't match configuration.

**Fix:**

1. Check storage health:
   ```bash
   ./deploy/up.sh local logs storage
   ```
2. Verify `FileStorage__BucketName` in `deploy/envs/local/compose.env` matches what the API expects
3. Access the MinIO Console at `http://localhost:<BASE_PORT+12>` (default credentials: `minioadmin` / `minioadmin`)

### Seq UI not loading

**Cause:** Seq (structured log viewer) is only included in the local Docker overlay. It may still be starting, or the port mapping is wrong.

**Fix:**

1. Check that Seq is running:
   ```bash
   ./deploy/up.sh local ps
   ```
2. Access Seq at `http://localhost:<BASE_PORT+8>` (default: `http://localhost:13008`)
3. If the container is not listed, verify that you're using the `local` environment — Seq is not included in the production overlay

---

## Backend Development

### `dotnet build` fails after pulling (`NU1xxx` / restore errors)

**Cause:** NuGet packages need to be restored after dependency changes.

**Fix:**

```bash
dotnet restore src/backend/MyProject.slnx
dotnet build src/backend/MyProject.slnx
```

### Tests fail with "connection refused"

**Cause:** No Docker containers are needed for tests. Both component tests and API integration tests use EF Core's InMemory database provider — each test gets a fresh GUID-named database. Caching and Hangfire are disabled via `appsettings.Testing.json`.

If you see "connection refused," something is incorrectly trying to reach PostgreSQL — usually a misconfigured test fixture or a service that was not replaced by the test infrastructure.

**Fix:** Verify you're running the correct test project and that `CustomWebApplicationFactory` (API tests) or `TestDbContextFactory` (component tests) is properly configured.

### EF Core model changes not reflected

**Cause:** In Development mode, the API calls `Database.Migrate()` on startup to apply pending migrations. If you changed an entity but didn't create a migration, the database schema won't match your model.

> If your deployment manages the schema externally (e.g., SQL scripts or a separate migration tool), this entry may not apply. See [Development — Database Migrations](development.md#database-migrations) for the full workflow.

**Fix:**

```bash
dotnet ef migrations add <MigrationName> \
  --project src/backend/MyProject.Infrastructure \
  --startup-project src/backend/MyProject.WebApi \
  --output-dir Persistence/Migrations
```

Restart the API — the migration applies automatically on startup.

### "Cannot access a disposed context" (`ObjectDisposedException`)

**Cause:** A missing `await` on an async EF Core call. The `DbContext` gets disposed before the query completes.

**Fix:** Ensure every EF Core call (`ToListAsync`, `FirstOrDefaultAsync`, `SaveChangesAsync`, etc.) is properly `await`-ed. Check for fire-and-forget patterns or missing `async` on method signatures.

### `TimeProvider` not injected

**Cause:** The application registers `TimeProvider.System` as a singleton. If your service constructor takes `TimeProvider`, DI resolves it automatically.

**Fix:**

- In application code: inject `TimeProvider` via constructor — it's already registered
- In tests: use `FakeTimeProvider` from `Microsoft.Extensions.Time.Testing`:
  ```csharp
  var timeProvider = new FakeTimeProvider(
      new DateTimeOffset(2025, 1, 15, 12, 0, 0, TimeSpan.Zero));
  ```
- Never use `DateTime.UtcNow` or `DateTimeOffset.UtcNow` directly — always go through the injected `TimeProvider`

---

## Frontend Development

### `pnpm install` fails or produces a different lockfile

**Cause:** The project pins pnpm via corepack (`packageManager` field in `package.json`). Using a system-installed pnpm or a different version causes lockfile conflicts. CI uses `--frozen-lockfile` and will reject mismatches.

**Fix:**

```bash
corepack enable
pnpm install
```

### `pnpm run check` shows ~30 Paraglide errors

**Cause:** Paraglide JS generates its output at build time into `src/lib/paraglide/`. Before building, these files don't exist and `svelte-check` reports missing imports. Even after compiling Paraglide, `svelte-check` may still report ~30 module-level errors from the generated output — these are expected and do not affect functionality.

**Fix:** Compile Paraglide to reduce noise:

```bash
cd src/frontend
pnpm exec paraglide-js compile --project ./project.inlang --outdir ./src/lib/paraglide
```

The remaining errors resolve during a full `pnpm run build`. These errors are safe to ignore during development.

### `v1.d.ts` is out of date

**Cause:** The API types file (`src/lib/api/v1.d.ts`) is generated from the running API's OpenAPI spec. When backend endpoints or DTOs change, it needs regeneration.

**Fix:**

1. Make sure the API is running (via Docker or locally)
2. Run:
   ```bash
   cd src/frontend
   pnpm run api:generate
   ```

This fetches `http://localhost:<API_PORT>/openapi/v1.json` and regenerates the types file. Never hand-edit `v1.d.ts`.

### `pnpm run api:generate` fails (`fetch failed` / `ECONNREFUSED`)

**Cause:** The script fetches the OpenAPI spec from the running API. If the API isn't running or the port is wrong, the fetch fails.

**Fix:**

1. Start the API via Docker (`./deploy/up.sh local up -d`) or locally from your IDE
2. Verify the API is responding:
   ```bash
   curl http://localhost:<API_PORT>/openapi/v1.json
   ```
3. If the port doesn't match, check the `api:generate` script in `src/frontend/package.json` and update the port

### Frontend can't find API when running locally (outside Docker)

**Cause:** `src/frontend/.env.local` is created by the init script and is gitignored. If you cloned an already-initialized repo, this file won't exist. It contains `API_URL` and `TURNSTILE_SITE_KEY`.

**Fix:**

```bash
cp src/frontend/.env.example src/frontend/.env.local
```

Then edit `.env.local` to set the correct API port.

### HMR not working in Docker

**Cause:** The local Docker setup bind-mounts `./src/frontend` to `/app` for hot module replacement. If the mount isn't working, file changes won't trigger rebuilds.

**Fix:**

1. Verify the volume mount exists in `deploy/docker-compose.local.yml`:
   ```yaml
   volumes:
     - ./src/frontend:/app
     - frontend_node_modules:/app/node_modules
   ```
2. On macOS, ensure `src/frontend` is in Docker Desktop's file sharing settings
3. Restart the frontend container:
   ```bash
   ./deploy/up.sh local restart frontend
   ```

### Styles not applying (Tailwind / logical CSS)

**Cause:** The project uses Tailwind CSS 4 with logical CSS properties. Physical directional classes (`ml-*`, `mr-*`, `pl-*`, `pr-*`) are not allowed.

**Fix:** Use logical equivalents:

- `ms-*` / `me-*` instead of `ml-*` / `mr-*` (margin)
- `ps-*` / `pe-*` instead of `pl-*` / `pr-*` (padding)

Run `pnpm run lint` to catch violations. See [frontend conventions](../src/frontend/AGENTS.md) for the full style guide.

### i18n key not found

**Cause:** Translation keys must exist in all locale files. The project ships with English (`en`) and Czech (`cs`).

**Fix:** Add the key to both files:

- `src/frontend/src/messages/en.json`
- `src/frontend/src/messages/cs.json`

English is the base locale. The path pattern is `./src/messages/{locale}.json` as defined in `project.inlang/settings.json`.

---

## Authentication & Authorization

### 401 after logging in successfully

**Cause:** The auth system uses HttpOnly JWT cookies. If the cookie isn't being set, common causes are:

- **Wrong domain / origin** — the API and frontend must share the same origin or be properly proxied through the BFF
- **SameSite / Secure flags** — in production, cookies require HTTPS. Locally, HTTP works because `SameSite=Lax` is the default
- **CORS misconfiguration** — the API has a CORS startup guard; check the allowed origins

**Fix:** Open browser DevTools → Network tab. Inspect the login response headers — look for `Set-Cookie`. If the cookie is present but not being sent on subsequent requests, check the `Domain`, `Path`, `SameSite`, and `Secure` attributes.

### Token refresh loops / unexpected redirect to login

**Cause:** The frontend auth middleware intercepts 401 responses and attempts a single token refresh (`POST /api/auth/refresh`). Key behaviors:

- Concurrent 401s share a single refresh attempt (deduplicated)
- After a successful refresh, only **idempotent** requests (`GET`, `HEAD`, `OPTIONS`) are automatically retried
- Non-idempotent requests (`POST`, `PUT`, `PATCH`, `DELETE`) that receive a 401 are **not retried** — the 401 propagates to the caller
- If the refresh itself fails, the user is redirected to `/login`

**Fix:** If you're seeing unexpected login redirects:

1. Check the refresh token cookie — it may have expired or been invalidated by reuse detection
2. For non-GET requests failing with 401, the user may need to retry the action manually after re-authentication
3. Check the API logs for refresh token reuse detection warnings — this indicates a potential security event

### Permission changes not taking effect

**Cause:** Permissions are encoded in the JWT access token. After changing a user's role or permissions, the old token remains valid until it expires. The security stamp system detects role changes, but only when the token is refreshed.

**Fix:** The user must log out and back in, or wait for the access token to expire and trigger a refresh. In development, access token lifetime is configurable via `Authentication__Jwt__AccessTokenLifetime` in `deploy/envs/local/api.env`. See [Security](security.md) for the full auth architecture.

---

## CI/CD

### Jobs are skipped

**Cause:** The CI pipeline uses [`dorny/paths-filter`](https://github.com/dorny/paths-filter) to detect which files changed:

| Job | Triggered by changes in |
|---|---|
| Backend | `src/backend/**`, `global.json` |
| Frontend | `src/frontend/**` |
| Docker | Dockerfiles, `.csproj`, `package.json`, `pnpm-lock.yaml` |

If your changes don't match these paths, the corresponding jobs won't run.

**Fix:** This is expected behavior — it keeps CI fast. If you need to force a run, push a trivial change to a matched path or re-run the workflow manually from GitHub Actions.

### Docker build fails in CI

**Cause:** Dockerfile paths or build contexts may not match what `docker.yml` expects.

**Fix:** Verify that your Dockerfile is in the expected location (`src/backend/Dockerfile` or `src/frontend/Dockerfile`) and that the build context matches the workflow configuration in `.github/workflows/docker.yml`.

### Coverage report not posted on PR

**Cause:** The coverage reporting action version may be incompatible or the required permissions are missing.

**Fix:** Check the GitHub Actions logs for the specific step. Verify the action version in `.github/workflows/ci.yml` and ensure the workflow has `pull-requests: write` permission.

---

## Production Deployment

### "Environment directory not found" (`deploy/envs/production/`)

**Cause:** The production environment directory doesn't exist yet. The template ships a `production-example` directory that you must copy and configure.

**Fix:**

```bash
cp -r deploy/envs/production-example deploy/envs/production     # Linux / macOS
Copy-Item -Recurse deploy\envs\production-example deploy\envs\production   # Windows PowerShell
```

Then edit `deploy/envs/production/compose.env` with your actual values.

### Container crashes immediately in production (`read_only` filesystem)

**Cause:** The production overlay applies `read_only: true` to all service containers for security hardening. Services that write to unexpected paths will crash with permission errors.

Only `/tmp` is writable by default. The API service also gets `/home/app` as a writable tmpfs mount (.NET needs it for data-protection keys and diagnostics). Stateful services (`db`, `storage`) override `read_only` back to `false`.

**Fix:** If a custom service needs to write to disk, either:

- Add a `tmpfs` mount for the writable path in `docker-compose.production.yml`
- Or set `read_only: false` on that service (less secure)

### SvelteKit CSRF errors in production (`403 Cross-site POST form submissions are forbidden`)

**Cause:** SvelteKit requires the `ORIGIN` environment variable when running behind a TLS-terminating reverse proxy. Without it, form submissions and non-GET requests are rejected.

**Fix:** Set `ORIGIN` in `deploy/envs/production/compose.env`:

```env
ORIGIN=https://your-domain.com
```

If you have multiple origins (e.g., `www` and apex domain), use `ALLOWED_ORIGINS` for the extras.

### Required production environment variables

These variables must be set in `deploy/envs/production/compose.env` before the stack will start:

| Variable | Purpose |
|---|---|
| `API_IMAGE` | Docker image for the API (e.g., `registry/my-api:1.0.0`) |
| `FRONTEND_IMAGE` | Docker image for the frontend |
| `POSTGRES_USER` | Database username |
| `POSTGRES_PASSWORD` | Database password |
| `JWT_SECRET_KEY` | Minimum 32 characters — generate with `openssl rand -base64 64` |
| `ORIGIN` | Public URL for SvelteKit CSRF (e.g., `https://your-domain.com`) |

See `deploy/envs/production-example/compose.env` for the full list with descriptions.

---

## General FAQ

### Which user should I log in as?

Three users are seeded from `deploy/envs/local/seed.env`:

| Role | Email | Password |
|---|---|---|
| SuperAdmin | `superadmin@test.com` | `SuperAdmin123!` |
| Admin | `admin@test.com` | `AdminUser123!` |
| User | `testuser@test.com` | `TestUser123!` |

Use SuperAdmin for full access. The User role has the most restrictive permissions — good for testing authorization guards.

### Where are the logs?

- **Structured logs (Seq):** `http://localhost:<BASE_PORT+8>` (default: `http://localhost:13008`)
- **Container logs:** `./deploy/up.sh local logs <service>` (e.g., `api`, `frontend`, `db`)
- **Follow logs in real time:** `./deploy/up.sh local logs -f api`

### How do I reset everything?

Tear down all containers and volumes, then rebuild:

```bash
./deploy/up.sh local down -v
./deploy/up.sh local up -d --build
```

The `-v` flag removes named volumes (database data, MinIO storage, Seq logs). Seeded users will be recreated on next startup.

### What's the difference between `up.sh` and `up.ps1`?

They are functionally identical — `up.sh` is for Linux/macOS (Bash), `up.ps1` is for Windows (PowerShell). Both are thin wrappers around `docker compose` that resolve the environment-specific compose overlay and env files. Use whichever matches your shell.

---

## Still Stuck?

If this guide didn't solve your problem:

1. **Search [existing GitHub issues](https://github.com/fpindej/netrock/issues)** — someone may have hit the same problem
2. **Ask in [Discord](https://discord.gg/5rHquRptSh)** — the community and maintainers are active
3. **Open a [new issue](https://github.com/fpindej/netrock/issues/new)** — include:
   - Your OS and Docker version
   - The full error message or log output
   - Steps to reproduce
