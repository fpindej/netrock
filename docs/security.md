# Security - Not an Afterthought

> Back to [README](../README.md)

NETrock is built **security-first**. Every decision defaults to the most restrictive option, then selectively opens what's needed.

## Authentication & Session Security

- **JWT in HttpOnly cookies** - tokens never touch JavaScript, immune to XSS theft
- **Refresh token rotation** - single-use tokens with automatic family revocation on reuse detection (stolen token revokes all sessions for that user)
- **Security stamp validation** - permission changes propagate to active sessions via SHA-256 hashed stamps in JWT claims, cached in-process for performance
- **Soft refresh** - role/permission changes invalidate access tokens but preserve refresh tokens, so users silently re-authenticate instead of getting force-logged-out
- **Remember me** - persistent refresh tokens with configurable expiry (default 7 days), non-persistent sessions (default 24 hours) cleared on browser close

## Two-Factor Authentication

- **TOTP-based** - standard RFC 6238 time-based one-time passwords, compatible with any authenticator app (Google Authenticator, Authy, 1Password, etc.)
- **Challenge token flow** - login returns a short-lived challenge token instead of full auth; client must submit valid TOTP code to complete authentication
- **Recovery codes** - 8 single-use recovery codes generated during setup, each usable exactly once for account recovery if the authenticator is lost
- **Admin disable** - admins with `users.manage_2fa` permission can disable 2FA for locked-out users, with automatic session revocation and notification email to the user
- **OAuth bypass** - OAuth logins skip 2FA verification since the identity provider has already authenticated the user

## OAuth / External Login Security

- **Manual code exchange** - NETrock implements the OAuth 2.0 authorization code flow directly via `HttpClient`, not ASP.NET middleware. This gives full control over token exchange, error handling, and user linking
- **State token CSRF protection** - every OAuth flow starts with a cryptographic state token stored in the database. The callback validates the token and immediately marks it as consumed to prevent replay
- **TOCTOU hardening** - state token consumption uses `UPDATE ... WHERE consumed = false` with row-count verification to prevent time-of-check/time-of-use race conditions
- **AES-256-GCM encrypted credentials** - OAuth provider client secrets are encrypted at rest using AES-256-GCM with a configurable encryption key. Credentials are only decrypted in-memory during the OAuth flow
- **Auto-link by verified email** - when a user signs in with an OAuth provider, the system links the external account to an existing local account if the email matches and is verified. No duplicate accounts
- **Test connection** - admins can validate OAuth provider credentials without a real user login. The test sends a dummy authorization code to the token endpoint and verifies the provider responds with the expected error (invalid grant) rather than a credentials error
- **Per-provider configuration** - each provider is independently configurable (client ID, secret, scopes, endpoints) and can be enabled/disabled from the admin UI without a redeploy

## Authorization & Access Control

- **Permission-based authorization** - atomic permissions (`users.view`, `users.manage`, `roles.manage`, ...) enforced on every endpoint via `[RequirePermission]`
- **Role hierarchy protection** - Superuser > Admin > User, with privilege escalation prevention (can't assign roles at or above your own rank, can't grant permissions you don't have)
- **Self-protection rules** - can't lock your own account, can't delete yourself, can't remove your own roles
- **System role guards** - Superuser/Admin/User cannot be deleted or renamed, Superuser permissions are implicit (never stored in DB)
- **Frontend mirrors backend** - route guards, nav filtering, and conditional rendering use the same permission claims, but the backend is always authoritative

## PII Compliance

- **`users.view_pii` permission** - personal data (email, phone) is only visible to users with this specific permission. All other users see masked values
- **Server-side masking** - PII is masked at the API layer, not the frontend. `j***@g***.com` for emails, `***` for phone numbers. No PII leaks through the API regardless of client
- **Self-view exemption** - users can always see their own data
- **No PII in logs, URLs, or errors** - email addresses, tokens, and personal data are never included in log output, URL parameters, or error responses

## Transport & Headers

- **CORS production safeguard** - startup guard rejects `AllowAllOrigins` in non-development environments
- **CSP with nonce mode** - script-src locked down, Turnstile CAPTCHA whitelisted explicitly
- **Security headers on every response** - `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy`, `Permissions-Policy`, HSTS in production
- **CSRF protection** - Origin header validation in the SvelteKit API proxy for all state-changing requests

## Rate Limiting & Input Validation

- **Rate limiting** - global + per-endpoint policies (registration has stricter limits), configurable per environment, with IP and user partitioning
- **Visible feedback** - rate-limited actions show countdown timers ("Wait Xs") instead of silently failing
- **Input validation everywhere** - FluentValidation on backend (even if frontend already validates), Data Annotations flowing into OpenAPI spec

## Data Protection & Audit

- **Full audit trail** - append-only `AuditEvents` table with JSONB metadata, 40 action constants covering login, registration, password changes, role modifications, OAuth connections, 2FA changes, and admin actions
- **Soft delete** - nothing is ever truly gone, every mutation tracked with who/when audit fields (`CreatedAt/By`, `UpdatedAt/By`, `DeletedAt/By`)
- **Dev config stripping** - `appsettings.Development.json` and `appsettings.Testing.json` excluded from production Docker images

## Reporting a Vulnerability

Found a security issue? Please report it privately - see [SECURITY.md](../SECURITY.md) for the full disclosure policy.
