# Development

> Back to [README](../README.md)

## Local Development (Aspire)

Aspire is the sole local development workflow. It starts all infrastructure (PostgreSQL, MinIO) as containers and launches the API and frontend dev server.

```bash
dotnet run --project src/backend/MyProject.AppHost
```

The Aspire Dashboard URL appears in the console. All service URLs (API docs, pgAdmin, MinIO) are linked from the Dashboard.

### Debugging with breakpoints in Rider/VS

Launch the AppHost project from your IDE. The API runs in-process with full debugger support. Infrastructure containers are still managed by Aspire.

### Configuration

Behavioral config (log levels, rate limits, JWT lifetimes, CORS, seed users) lives in `appsettings.Development.json`. Infrastructure connection strings are injected by Aspire via environment variables — no manual config needed.

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
