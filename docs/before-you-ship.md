# Before You Ship

> Back to [README](../README.md)

NETrock works out of the box for local development, but there are things you need to configure before going to production. This checklist covers what the template **can't decide for you**.

## Must Do

- [ ] **Email service** — replace `NoOpEmailService` with a real provider (SMTP, SendGrid, Postmark, etc.). The NoOp service just logs emails to Seq. Configure via `Email__Smtp__*` env vars or swap the service registration in `ServiceCollectionExtensions.cs`
- [ ] **CORS origins** — set `Cors__AllowedOrigins` to your production domain(s). The app **will refuse to start** if `AllowAllOrigins` is `true` outside of Development — this is intentional
- [ ] **JWT secret** — the init script generates one, but verify it's set in production via `Authentication__Jwt__Key` (64+ chars, cryptographically random)
- [ ] **Database** — point `ConnectionStrings__Database` to your production PostgreSQL instance
- [ ] **CAPTCHA keys** — replace the Cloudflare Turnstile development keys with production keys (`Captcha__SecretKey` backend, `PUBLIC_TURNSTILE_SITE_KEY` frontend)
- [ ] **Frontend URL in emails** — set `Email__FrontendBaseUrl` to your production domain so email verification and password reset links work

## Should Do

- [ ] **Redis** — enable for production (`Caching__Redis__Enabled=true`) with real credentials. Without it, the app falls back to in-memory cache (fine for single-instance, not for scaling)
- [ ] **Reverse proxy** — if behind nginx/load balancer, configure `Hosting__ReverseProxy__TrustedNetworks` and `TrustedProxies` so rate limiting uses real client IPs
- [ ] **Logging** — replace Seq with your production logging solution or point Serilog at your provider. Adjust log levels (`Serilog__MinimumLevel__Default=Information`)
- [ ] **Rate limits** — review the production defaults in `appsettings.json` and adjust for your expected traffic
- [ ] **Backups** — set up automated PostgreSQL backups. NETrock uses soft delete, but that doesn't replace real backups
- [ ] **Monitoring** — the health check endpoints (`/health`, `/health/ready`, `/health/live`) are ready for your uptime monitoring

## Good to Know

- **Hangfire dashboard** is automatically disabled in production. Use the admin API endpoints (`/api/admin/jobs/*`) instead
- **HTTPS** is forced in production via `Hosting__ForceHttps=true` (default). Development runs on HTTP
- **Dev config is stripped** from production Docker images — `appsettings.Development.json` and `appsettings.Testing.json` are removed at build time
- **CORS startup guard** will crash the app on purpose if you deploy with `AllowAllOrigins=true` — this is a security feature, not a bug
