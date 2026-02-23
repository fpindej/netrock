# Development

> Back to [README](../README.md)

## Developer Workflows

### Frontend dev — tweak backend config without touching code

Edit `deploy/envs/local/api.env`, restart Docker:

```bash
# Longer JWT tokens, relaxed rate limit
Authentication__Jwt__AccessTokenLifetime=05:00:00
RateLimiting__Global__PermitLimit=1000
```

```bash
./deploy/up.sh local up -d
```

### Backend dev — debug with breakpoints in Rider/VS

1. Stop the API container: `./deploy/up.sh local stop api`
2. Set `API_URL=http://host.docker.internal:5142` in `deploy/envs/local/compose.env`
3. Restart frontend: `./deploy/up.sh local restart frontend`
4. Launch API from your IDE — breakpoints work, frontend proxies to it

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

## Deployment

Build and push Docker images with semantic versioning:

```bash
./deploy/build.sh backend --minor    # Build, bump minor version, push to registry
./deploy/build.sh frontend --patch   # Same for frontend
```

Supports Docker Hub, GitHub Container Registry, Azure ACR, AWS ECR, DigitalOcean, and custom registries.
