# OAuth Provider Test Connection Button

**Date**: 2026-03-02
**Scope**: Add a Test Connection button to the admin OAuth provider configuration page

## Summary

Added a "Test Connection" button to the admin OAuth provider card that validates stored credentials against the provider's token endpoint without requiring a real user login. The feature sends a deliberately invalid authorization code and interprets the error response to distinguish valid credentials from invalid ones.

## Changes Made

| File | Change | Reason |
|------|--------|--------|
| `IExternalAuthProvider.cs` | Added `TestConnectionAsync` method | Interface contract for provider-level credential validation |
| `GoogleAuthProvider.cs` | Implemented test connection | POST to `oauth2.googleapis.com/token` with dummy code, check `error` field |
| `GitHubAuthProvider.cs` | Implemented test connection | GitHub returns 200 even on errors, check `error` field for `incorrect_client_credentials` vs `bad_verification_code` |
| `DiscordAuthProvider.cs` | Implemented test connection | POST to `discord.com/api/oauth2/token`, check `error` field |
| `AppleAuthProvider.cs` | Implemented test connection | POST to `appleid.apple.com/auth/token`, check `error` field |
| `IProviderConfigService.cs` | Added `TestConnectionAsync` to application interface | Exposes test functionality to controllers |
| `ProviderConfigService.cs` | Implemented test orchestration | Loads credentials directly from DB (bypasses enabled filter), decrypts, delegates to provider, logs audit |
| `ErrorMessages.cs` | Added 3 test connection error messages | `TestConnectionInvalidCredentials`, `TestConnectionProviderUnreachable`, `TestConnectionNotConfigured` |
| `AuditActions.cs` | Added `AdminTestOAuthProvider` | Audit trail for credential test events |
| `OAuthProvidersController.cs` | Added `POST .../test` endpoint | 204 on success, ProblemDetails on failure, rate limited |
| `v1.d.ts` | Added type definition for new endpoint | Frontend type safety |
| `OAuthProviderCard.svelte` | Added Test Connection button with loading state | Outline variant before Save, disabled when no credentials configured |
| `audit.ts` | Added label and variant mapping | Display audit events correctly |
| `en.json` / `cs.json` | Added 4 translation keys each | Button label, success/error toasts, audit action label |

## Decisions & Reasoning

### Dummy Code Strategy

- **Choice**: Send a dummy authorization code (`__test_connection__`) to the token endpoint
- **Alternatives considered**: (1) Call a discovery/metadata endpoint, (2) Attempt a client_credentials grant
- **Reasoning**: The token endpoint is the only endpoint that validates both client_id and client_secret together. Discovery endpoints are unauthenticated. The `client_credentials` grant is not supported by all providers. With a dummy code, valid credentials get `invalid_grant` (code is bad but client is accepted), while invalid credentials get `invalid_client` (client rejected).

### GitHub 200-for-Errors Quirk

- **Choice**: Parse the JSON body `error` field even on HTTP 200 responses
- **Alternatives considered**: Relying solely on HTTP status codes
- **Reasoning**: GitHub's token endpoint returns HTTP 200 with an `error` field in the body for all error cases. The provider checks for `incorrect_client_credentials` (bad creds) vs `bad_verification_code` (valid creds, bad code).

### Bypass Enabled Filter

- **Choice**: Load credentials directly from DB instead of using `GetCredentialsAsync`
- **Alternatives considered**: Reusing the cached `GetCredentialsAsync` method
- **Reasoning**: `GetCredentialsAsync` filters by `IsEnabled` and returns null for disabled providers. Admins need to test credentials before enabling a provider, so the test must work regardless of enabled state.

## Follow-Up Items

- [ ] Regenerate `v1.d.ts` from running API server to ensure type definitions match exactly (manually added during this session)
