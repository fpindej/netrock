# Refactor Frontend Auth Endpoints

**Date**: 2026-02-21
**Scope**: Frontend authentication client refactoring — middleware-based token refresh, error handling, session expiry UX

## Summary

Replaced the hand-rolled `fetchWithAuth` wrapper in the API client with openapi-fetch's built-in middleware API (`onResponse` hook). This cleanly separates auth concerns from API calls, adds proper session expiry handling with toast notifications and login redirect, and distinguishes backend outages from authentication failures.

## Changes Made

| File | Change | Reason |
|------|--------|--------|
| `src/frontend/src/lib/api/client.ts` | Replaced `fetchWithAuth` wrapper with `createAuthMiddleware` using openapi-fetch `Middleware` type. Added `setAuthFailureHandler` for navigation-aware auth failure callback. | Clean separation of auth logic via the library's middleware API instead of wrapping fetch |
| `src/frontend/src/lib/api/index.ts` | Added `setAuthFailureHandler` to barrel export | Allow root layout to wire up auth failure handling |
| `src/frontend/src/lib/auth/auth.ts` | `getUser()` now returns `GetUserResult` with `{ user, error }` instead of `User \| null`. Added `GetUserResult` interface. | Distinguish "not authenticated" from "backend unavailable" |
| `src/frontend/src/lib/auth/index.ts` | Added `GetUserResult` type re-export | Barrel export consistency |
| `src/frontend/src/routes/+layout.server.ts` | Destructures `{ user, error }` from `getUser()`, passes `backendError` to children | Propagate backend error state to child layouts |
| `src/frontend/src/routes/(app)/+layout.server.ts` | Added `backendError` check → throws 503 before redirect check | Prevent misleading redirect to login when backend is down |
| `src/frontend/src/routes/+layout.svelte` | Added `setAuthFailureHandler` call in `onMount` with toast + `invalidateAll` + `goto('/login')` | Wire up session expiry UX — toast notification and redirect |
| `src/frontend/src/routes/api/[...path]/+server.ts` | Renamed `COOKIE_AUTH_ENDPOINTS` → `COOKIE_AUTH_PATHS` with expanded docs | Clarity — the list controls `?useCookies=true` injection, not generic auth |
| `src/frontend/src/messages/en.json` | Added `auth_sessionExpired_title` and `auth_sessionExpired_description` | i18n for session expiry toast |
| `src/frontend/src/messages/cs.json` | Added Czech translations for session expiry keys | i18n parity |
| `src/frontend/AGENTS.md` | Updated API Client architecture and Authentication Flow sections | Reflect middleware-based architecture and `backendError` propagation |

## Decisions & Reasoning

### Use openapi-fetch middleware instead of custom fetch wrapper

- **Choice**: Adopted `client.use(middleware)` with `onResponse` hook
- **Alternatives considered**: (a) Switch to `@hey-api/openapi-ts` for a batteries-included SDK with built-in interceptors, (b) Keep the `fetchWithAuth` wrapper but improve it
- **Reasoning**: The project already uses openapi-fetch + openapi-typescript for type generation. The middleware API is a first-class feature of the library since v0.9+. Switching libraries would require migrating 24+ files that import `browserClient`. The middleware approach gave us the exact hooks needed with zero migration cost.

### Only retry idempotent methods after token refresh

- **Choice**: After a successful refresh, only auto-retry GET/HEAD/OPTIONS. Non-idempotent methods (POST/PUT/PATCH/DELETE) return the original 401.
- **Alternatives considered**: Retry all methods, or buffer request bodies for safe replay
- **Reasoning**: Automatically retrying a POST after refresh could cause double-submission (e.g., creating two orders). The caller already has the context to retry manually. This matches industry best practices for auth interceptors.

### Module-level `setAuthFailureHandler` instead of constructor parameter

- **Choice**: `browserClient` is created at module scope and auth failure handling is wired later via `setAuthFailureHandler()`
- **Alternatives considered**: Lazy-initialize `browserClient` via `initBrowserClient()` called from the root layout
- **Reasoning**: 24 files import `browserClient` at the top level. Making it lazy (`let browserClient`) would require either null checks everywhere or `!` assertions. The settable callback pattern keeps `browserClient` always defined while allowing the root layout to wire up navigation-dependent behavior in `onMount`.

### Structured `GetUserResult` over nullable `User`

- **Choice**: Return `{ user, error }` from `getUser()` instead of `User | null`
- **Alternatives considered**: Throw errors for backend failures, use separate error codes
- **Reasoning**: `User | null` conflates two very different states: "not logged in" and "backend is down". The structured result lets `(app)/+layout.server.ts` show a 503 error page instead of incorrectly redirecting to login during outages.

## Diagrams

```mermaid
sequenceDiagram
    participant B as Browser
    participant M as Auth Middleware
    participant P as API Proxy
    participant API as Backend

    B->>M: GET /api/users/me
    M->>P: Forward request
    P->>API: Forward + cookies
    API-->>P: 401 Unauthorized
    P-->>M: 401 Response

    Note over M: onResponse intercepts 401

    M->>P: POST /api/auth/refresh
    P->>API: Forward + ?useCookies=true
    API-->>P: 200 + Set-Cookie (new tokens)
    P-->>M: 200 OK

    Note over M: Refresh succeeded, method is GET (idempotent)

    M->>P: Retry GET /api/users/me
    P->>API: Forward + new cookies
    API-->>P: 200 + user data
    P-->>M: 200 Response
    M-->>B: User data
```

```mermaid
sequenceDiagram
    participant B as Browser
    participant M as Auth Middleware
    participant P as API Proxy
    participant API as Backend

    B->>M: POST /api/users/me (update profile)
    M->>P: Forward request
    P->>API: Forward + cookies
    API-->>P: 401 Unauthorized
    P-->>M: 401 Response

    Note over M: onResponse intercepts 401

    M->>P: POST /api/auth/refresh
    P->>API: Forward + ?useCookies=true
    API-->>P: 401 (refresh token expired)
    P-->>M: 401 Failure

    Note over M: Refresh failed → onAuthFailure

    M->>B: toast.error("Session expired")
    M->>B: invalidateAll() + goto('/login')
```

## Follow-Up Items

- [ ] Consider adding a 503 error page (`+error.svelte`) with a "retry" button for backend outage UX
- [ ] Consider adding a loading skeleton or spinner while the refresh attempt is in-flight
- [ ] Monitor whether concurrent 401 deduplication handles edge cases in production (e.g., tab switching, background tabs)
