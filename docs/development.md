# Development

> Back to [README](../README.md)

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

---

## Database Migrations

```bash
dotnet ef migrations add <Name> \
  --project src/backend/<YourProject>.Infrastructure \
  --startup-project src/backend/<YourProject>.WebApi \
  --output-dir Features/Postgres/Migrations
```

Migrations auto-apply on startup in Development.

---

## Deployment

Build and push Docker images with semantic versioning:

```bash
./deploy.sh backend --minor    # Build, bump minor version, push to registry
./deploy.sh frontend --patch   # Same for frontend
```

Supports Docker Hub, GitHub Container Registry, Azure ACR, AWS ECR, DigitalOcean, and custom registries.
