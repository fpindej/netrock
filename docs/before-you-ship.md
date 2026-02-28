# Before You Ship

> Back to [README](../README.md)

NETrock works out of the box for local development, but there are things you need to configure before going to production. This checklist covers what the template **can't decide for you**.

> **Start from the production template:** Copy `deploy/envs/production-example` to `deploy/envs/production` (`cp -r deploy/envs/production-example deploy/envs/production`) and fill in all values. Launch with `./deploy/up.sh production up -d`.

## Must Do

- [ ] **Email service** — replace `NoOpEmailService` with a real provider (SMTP, SendGrid, Postmark, etc.). The NoOp service just logs emails to the console. Configure via `Email__Smtp__*` env vars or swap the service registration in `ServiceCollectionExtensions.cs`
- [ ] **CORS origins** — set `Cors__AllowedOrigins` to your production domain(s). The app **will refuse to start** if `AllowAllOrigins` is `true` outside of Development — this is intentional
- [ ] **JWT secret** — `appsettings.json` ships with a dev-only placeholder key. Set `JWT_SECRET_KEY` in production `compose.env` (minimum 32 chars, cryptographically random)
- [ ] **Database** — point `ConnectionStrings__Database` to your production PostgreSQL instance
- [ ] **CAPTCHA keys** — replace the Cloudflare Turnstile development keys with production keys (`Captcha__SecretKey` backend, `TURNSTILE_SITE_KEY` frontend — runtime-configurable via the `(public)` layout server load)
- [ ] **Frontend URL in emails** — set `Email__FrontendBaseUrl` to your production domain so email verification and password reset links work
- [ ] **Bootstrap admin** — uncomment and fill in `Seed__Users__*` entries in `deploy/envs/production/seed.env` to create an initial SuperAdmin on first deploy. Idempotent — safe to leave set, but remove after creating admin accounts through the UI
- [ ] **File storage** — configure `FileStorage__*` env vars for your S3-compatible provider. Local dev uses MinIO (included in Docker stack). For production, point to your preferred provider — AWS S3, Cloudflare R2, DigitalOcean Spaces, Backblaze B2, or any S3-compatible service. Set `FileStorage__Endpoint`, `FileStorage__AccessKey`, `FileStorage__SecretKey`, `FileStorage__BucketName`, `FileStorage__Region` (if applicable), and `FileStorage__UseSSL=true`. See `SKILLS.md` → "Swap S3 Provider" for provider-specific configs. If you don't need file storage, see `SKILLS.md` → "Remove File Storage Entirely"

## Should Do

- [ ] **TLS termination** — the production compose exposes API (8080) and frontend (3000) as plain HTTP. Put a reverse proxy (nginx, Caddy, Traefik) in front to terminate TLS. Set `ORIGIN=https://your-domain.com` in the frontend env so SvelteKit generates correct URLs. Example with Caddy:
  ```
  your-domain.com {
      reverse_proxy frontend:3000
  }
  api.your-domain.com {
      reverse_proxy api:8080
  }
  ```
- [ ] **Reverse proxy** — if behind nginx/load balancer, configure `Hosting__ReverseProxy__TrustedNetworks` and `TrustedProxies` so rate limiting uses real client IPs
- [ ] **Logging** — logs flow via OpenTelemetry. Set `OTEL_EXPORTER_OTLP_ENDPOINT` for your production collector (Grafana, Datadog, etc.). Locally, logs are visible in the Aspire Dashboard. Adjust log levels (`Serilog__MinimumLevel__Default=Information`)
- [ ] **Rate limits** — review the production defaults in `appsettings.json` and adjust for your expected traffic
- [ ] **Backups** — set up automated PostgreSQL backups. NETrock uses soft delete, but that doesn't replace real backups. Quick manual backup via compose:
  ```bash
  ./deploy/up.sh production exec db pg_dump -U $POSTGRES_USER $POSTGRES_DB > backup.sql
  ```
- [ ] **Monitoring** — the health check endpoints (`/health`, `/health/ready`, `/health/live`) are ready for your uptime monitoring
- [ ] **Resource limits** — the production compose ships with reasonable defaults (API: 2 CPU / 1G, frontend: 1 CPU / 512M, Postgres: 1 CPU / 1G). Tune these in `deploy/docker-compose.production.yml` for your workload — PostgreSQL alone typically wants 25% of available memory for `shared_buffers`

## Good to Know

- **Hangfire dashboard** is automatically disabled in production. Use the admin API endpoints (`/api/admin/jobs/*`) instead
- **HTTPS** is forced in production via `Hosting__ForceHttps=true` (default). Development runs on HTTP
- **Dev config is stripped** from production Docker images — `appsettings.Development.json` and `appsettings.Testing.json` are removed at build time
- **CORS startup guard** will crash the app on purpose if you deploy with `AllowAllOrigins=true` — this is a security feature, not a bug
- **Volume migration** — if upgrading from the pre-`deploy/` layout, Docker named volumes will get new project-prefixed names. Your old data stays in the old volumes. To migrate: stop the old stack, `docker volume create` the new names, and copy data across (or start fresh for dev)
