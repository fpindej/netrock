# Troubleshooting & FAQ

> Back to [README](../README.md)

## Init Script

### Permission denied running `init.sh`

**Cause:** The script doesn't have execute permission.

**Fix:**
```bash
chmod +x init.sh
./init.sh
```

### Name validation fails

**Cause:** The project name must match `^[A-Z][a-zA-Z0-9]*$` — PascalCase, starting with an uppercase letter, alphanumeric only. No hyphens, underscores, or spaces.

**Valid:** `MyAwesomeApi`, `TodoApp`, `WebApi`
**Invalid:** `my-app`, `todo_app`, `123App`

### Port conflict during init

**Cause:** The base port (default `13000`) or one of its offsets is already in use. The template allocates 7 ports from your base:

| Offset | Service |
|---|---|
| +0 | Frontend |
| +2 | API |
| +4 | PostgreSQL |
| +6 | Redis |
| +8 | Seq |
| +10 | MinIO |
| +12 | MinIO Console |

**Fix:** Check what's using the port and pick a different base:
```bash
lsof -i :13000
```

Choose a base port between 1024 and 65530 that leaves enough room for all offsets.

### `dotnet-ef` not found

**Cause:** The EF Core CLI tool isn't installed or restored.

**Fix:**
```bash
dotnet tool restore
```

This restores tools declared in `.config/dotnet-tools.json`, including `dotnet-ef`.

### Init succeeds but Docker fails to start

**Cause:** Missing prerequisites or Docker daemon not running.

**Fix:** Verify all prerequisites are available:
```bash
git --version
dotnet --version
docker info
node --version
pnpm --version    # or: corepack enable && pnpm --version
```

If `docker info` fails, start Docker Desktop and try again.

---

## Docker & Compose

### Container keeps restarting

**Cause:** A service is crashing on startup. Common reasons: missing environment variables, database not ready, or configuration errors.

**Fix:** Check the logs for the failing service:
```bash
./deploy/up.sh local logs api
./deploy/up.sh local logs frontend
```

`./deploy/up.sh` is a thin wrapper around `docker compose` — all standard subcommands work (`logs`, `ps`, `restart`, `down`, etc.).

### Database health check failing

**Cause:** PostgreSQL can be slow to initialize, especially on first run. The health check (`pg_isready`) has a `start_period` of 15 seconds and retries every 5 seconds with 5 retries.

**Fix:** Wait 30–60 seconds on first startup. If it still fails:
```bash
./deploy/up.sh local logs db
```

Look for disk space issues, permission errors, or invalid `POSTGRES_USER`/`POSTGRES_PASSWORD` in `deploy/envs/local/compose.env`.

### Frontend can't reach the API

**Cause:** The frontend container connects to the API via Docker's internal network using the `API_URL` environment variable (default: `http://api:8080`). The frontend is on the `frontend` network, and the API bridges both `frontend` and `backend` networks.

**Fix:**
1. Verify the API container is running and healthy: `./deploy/up.sh local ps`
2. Check `API_URL` in `deploy/envs/local/compose.env` — it should be `http://api:8080` for Docker
3. If running the API outside Docker (e.g., from your IDE), set `API_URL=http://host.docker.internal:5142` and restart the frontend container

### Port already in use

**Cause:** Another Docker stack or host service is bound to the same port.

**Fix:**
```bash
lsof -i :<port>
```

Either stop the conflicting process or change the port in `deploy/envs/local/compose.env`.

### `node_modules` volume is stale

**Cause:** The local Docker setup uses a named volume (`frontend_node_modules`) for `/app/node_modules` to prevent the host bind mount from overwriting container dependencies. After dependency changes, this volume can become outdated.

**Fix:**
```bash
./deploy/up.sh local down
docker volume rm $(docker volume ls -q | grep frontend_node_modules)
./deploy/up.sh local up -d --build
```

### MinIO bucket not created

**Cause:** The storage service didn't initialize properly, or the bucket name doesn't match configuration.

**Fix:**
1. Check storage health: `./deploy/up.sh local logs storage`
2. Verify `FileStorage__BucketName` in `deploy/envs/local/compose.env` matches what the API expects
3. The MinIO Console is available at `http://localhost:<BASE_PORT+12>` (default credentials: `minioadmin` / `minioadmin`)

---

## Backend Development

### `dotnet build` fails after pulling

**Cause:** NuGet packages need to be restored after dependency changes.

**Fix:**
```bash
dotnet restore src/backend/MyProject.slnx
dotnet build src/backend/MyProject.slnx
```

### Tests fail with "connection refused"

**Cause:** Tests do **not** connect to Docker. Both component tests and API integration tests use EF Core's InMemory database provider. Each test gets a fresh database (GUID-named). If you see "connection refused," something is incorrectly trying to reach PostgreSQL or Redis.

**Fix:** Check that you're running the correct test project. The test infrastructure explicitly removes Npgsql registrations and substitutes InMemory. Redis and Hangfire are disabled via `appsettings.Testing.json`. No Docker containers need to be running for tests.

### EF Core model changes not reflected

**Cause:** In Development mode, the API calls `Database.Migrate()` on startup, which applies pending migrations. If you changed an entity but didn't create a migration, the database schema won't match.

**Fix:**
```bash
dotnet ef migrations add <MigrationName> \
  --project src/backend/<YourProject>.Infrastructure \
  --startup-project src/backend/<YourProject>.WebApi \
  --output-dir Persistence/Migrations
```

Then restart the API — the migration applies automatically.

### "Cannot access a disposed context"

**Cause:** A missing `await` on an async EF Core call. The `DbContext` gets disposed before the query completes.

**Fix:** Ensure every EF Core call (`ToListAsync`, `FirstOrDefaultAsync`, `SaveChangesAsync`, etc.) is properly `await`-ed. Check for fire-and-forget patterns or missing `async` on method signatures.

### `TimeProvider` not injected

**Cause:** The application registers `TimeProvider.System` as a singleton. If your service constructor takes `TimeProvider`, DI resolves it automatically.

**Fix:**
- In production/application code: inject `TimeProvider` via constructor — it's already registered
- In tests: use `FakeTimeProvider` from `Microsoft.Extensions.Time.Testing`:
  ```csharp
  var timeProvider = new FakeTimeProvider(
      new DateTimeOffset(2025, 1, 15, 12, 0, 0, TimeSpan.Zero));
  ```
- Never use `DateTime.UtcNow` or `DateTimeOffset.UtcNow` directly — always go through the injected `TimeProvider`

---

## Frontend Development

### `pnpm run check` shows Paraglide errors

**Cause:** Paraglide JS generates its output at build time into `src/lib/paraglide/`. If you haven't built yet, these files don't exist and `svelte-check` reports missing imports.

**Fix:** Compile Paraglide first:
```bash
cd src/frontend
pnpm exec paraglide-js compile --project ./project.inlang --outdir ./src/lib/paraglide
```

This happens automatically during `pnpm run build` and in CI.

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

### HMR not working in Docker

**Cause:** The local Docker setup bind-mounts `./src/frontend` to `/app` for hot module replacement. If the mount isn't working, file changes won't trigger rebuilds.

**Fix:**
1. Verify the volume mount exists in `deploy/docker-compose.local.yml`:
   ```yaml
   volumes:
     - ./src/frontend:/app
     - frontend_node_modules:/app/node_modules
   ```
2. On macOS, ensure the `src/frontend` directory is in Docker Desktop's file sharing settings
3. Restart the frontend container: `./deploy/up.sh local restart frontend`

### Styles not applying

**Cause:** The project uses Tailwind CSS 4 with logical CSS properties. Physical directional classes are not allowed.

**Fix:** Use logical equivalents:
- `ms-*` / `me-*` instead of `ml-*` / `mr-*`
- `ps-*` / `pe-*` instead of `pl-*` / `pr-*`

Run `pnpm run lint` to catch violations.

### i18n key not found

**Cause:** Translation keys must exist in all locale files. The project ships with English (`en`) and Czech (`cs`).

**Fix:** Add the key to both files:
- `src/frontend/src/messages/en.json`
- `src/frontend/src/messages/cs.json`

English is the base locale. The path pattern is `./src/messages/{locale}.json` as defined in `project.inlang/settings.json`.

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

**Cause:** Dockerfile paths or build contexts may not match what the `docker.yml` workflow expects.

**Fix:** Verify that your Dockerfile is in the expected location (`src/backend/Dockerfile` or `src/frontend/Dockerfile`) and that the build context matches the workflow configuration.

### Coverage report not posted on PR

**Cause:** The coverage reporting action version may be incompatible or the required permissions are missing.

**Fix:** Check the GitHub Actions logs for the specific step. Verify the action version in `.github/workflows/ci.yml` and ensure the workflow has `pull-requests: write` permission.

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

The `-v` flag removes named volumes (database data, Redis data, MinIO storage, Seq logs). Seeded users will be recreated on next startup.
