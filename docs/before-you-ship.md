# Before You Ship

> Back to [README](../README.md)

NETrock works out of the box for local development, but there are things you need to configure before going to production. This checklist covers what the template **can't decide for you**.

> **Environment variables** are the primary configuration mechanism. Set them on your API and frontend containers however your platform supports it (env files, UI, secrets manager, etc.). The checklist below lists what needs configuring.

## Must Do

- [ ] **Email service** - replace `NoOpEmailService` with a real provider (SMTP, SendGrid, Postmark, etc.). The NoOp service just logs emails to the console. Configure via `Email__Smtp__*` env vars or swap the service registration in `ServiceCollectionExtensions.cs`
- [ ] **CORS origins** - set `Cors__AllowedOrigins__0` to your production domain (add `__1`, `__2` for additional origins). The app **will refuse to start** if `AllowAllOrigins` is `true` outside of Development - this is intentional
- [ ] **JWT secret** - the init script generates a random 64-char key in `appsettings.json`. For production, set `Authentication__Jwt__Key` as an environment variable (minimum 32 chars, cryptographically random)
- [ ] **Database** - point `ConnectionStrings__Database` to your production PostgreSQL instance
- [ ] **CAPTCHA keys** - replace the Cloudflare Turnstile development keys with production keys (`Captcha__SecretKey` backend, `TURNSTILE_SITE_KEY` frontend - runtime-configurable via the `(public)` layout server load)
- [ ] **Frontend URL in emails** - set `Email__FrontendBaseUrl` to your production domain so email verification and password reset links work
- [ ] **Bootstrap admin** - set `Seed__Users__0__Email`, `Seed__Users__0__Password`, and `Seed__Users__0__Role=SuperAdmin` environment variables to create an initial SuperAdmin on first deploy. Idempotent - safe to leave set, but remove after creating admin accounts through the UI
- [ ] **File storage** - configure `FileStorage__*` env vars for your S3-compatible provider. Local dev uses MinIO (included in Aspire). For production, point to your preferred provider - AWS S3, Cloudflare R2, DigitalOcean Spaces, Backblaze B2, or any S3-compatible service. Set `FileStorage__Endpoint`, `FileStorage__AccessKey`, `FileStorage__SecretKey`, `FileStorage__BucketName`, `FileStorage__Region` (if applicable), and `FileStorage__UseSSL=true`. Use `/manage-file-storage` skill for provider-specific configs or to remove file storage entirely
- [ ] **OAuth encryption key** - set `Authentication__OAuth__EncryptionKey` to a cryptographically random base64 string (32+ bytes). This key encrypts OAuth provider client secrets at rest with AES-256-GCM. Generate with `openssl rand -base64 32`. Without this, OAuth provider management will not work in production
- [ ] **OAuth providers** - configure OAuth providers from the admin UI after first deploy. Each provider needs a client ID and secret from the provider's developer console (Google Cloud Console, GitHub Developer Settings, etc.). Admins can enable/disable providers, test connections, and update credentials without redeploying

## Should Do

- [ ] **TLS termination** - the containers expose API (8080) and frontend (3000) as plain HTTP. Put a reverse proxy (nginx, Caddy, Traefik) in front to terminate TLS, or use your platform's built-in TLS (Coolify, Railway, etc.). Set `ORIGIN=https://your-domain.com` in the frontend env so SvelteKit generates correct URLs
- [ ] **Reverse proxy** - if behind nginx/load balancer, configure `Hosting__ReverseProxy__TrustedNetworks` and `TrustedProxies` so rate limiting uses real client IPs
- [ ] **Logging** - logs flow via OpenTelemetry. Set `OTEL_EXPORTER_OTLP_ENDPOINT` for your production collector (Grafana, Datadog, etc.). Locally, logs are visible in the Aspire Dashboard. Adjust log levels (`Serilog__MinimumLevel__Default=Information`)
- [ ] **Rate limits** - review the production defaults in `appsettings.json` and adjust for your expected traffic
- [ ] **Backups** - set up automated PostgreSQL backups. NETrock uses soft delete, but that doesn't replace real backups
- [ ] **Monitoring** - the health check endpoints (`/health`, `/health/ready`, `/health/live`) are ready for your uptime monitoring
- [ ] **Resource limits** - configure CPU/memory limits in your deployment platform. Recommended starting points: API 2 CPU / 1G, frontend 1 CPU / 512M, PostgreSQL 1 CPU / 1G. PostgreSQL alone typically wants 25% of available memory for `shared_buffers`

## Good to Know

- **Hangfire dashboard** is automatically disabled in production. Use the admin API endpoints (`/api/admin/jobs/*`) instead
- **HTTPS** is forced in production via `Hosting__ForceHttps=true` (default). Development runs on HTTP
- **Dev config is stripped** from production Docker images - `appsettings.Development.json` and `appsettings.Testing.json` are removed at build time
- **CORS startup guard** will crash the app on purpose if you deploy with `AllowAllOrigins=true` - this is a security feature, not a bug
