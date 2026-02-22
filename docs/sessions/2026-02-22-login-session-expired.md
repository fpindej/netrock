# Login Session Expired Fix

**PR:** #244 (`fix/login-session-expired`)
**Date:** 2026-02-22
**Stacked on:** #242 (`refactor/deployment-profiles`)

## Problem

Two bugs on the login page after the deployment profile refactor:

1. **Session expired redirect never fired.** Users with expired sessions were redirected to `/login` without `?reason=session_expired`, so the toast never appeared.

2. **SSR crash on `/login?reason=session_expired`.** Calling `replaceState` during server-side rendering threw "Cannot call replaceState before router is initialized", causing a 500.

3. **"API is offline" shown on redirect.** The single-shot health check in `LoginForm` could fail transiently during the redirect flow, permanently disabling the login button.

## Root cause

### Cookie store mutation (bug 1)

SvelteKit's internal `fetch` intercepts `Set-Cookie` headers from responses and updates a shared cookie store via `set_internal()`. When the root layout's `getUser()` triggered a failed token refresh, the backend responded with `Set-Cookie: __Secure-REFRESH-TOKEN=; Max-Age=0`. After `getUser()` returned, `cookies.get(REFRESH_TOKEN_COOKIE)` checked the mutated store first and returned `undefined` — the cookie appeared gone even though the browser had sent it.

**Source code path:** `@sveltejs/kit/src/runtime/server/cookie.js` → `get()` checks `new_cookies` (mutated by fetch) before `header_cookies` (original request).

### replaceState during SSR (bug 2)

`replaceState` from `$app/navigation` requires the SvelteKit client-side router to be initialized. During SSR, the router doesn't exist. The fix was to move the call inside `onMount`.

### Transient health check failure (bug 3)

The `LoginForm` ran a single `fetch('/api/health')` in `onMount`. During the session-expired redirect flow, the backend could be briefly processing the failed refresh when the health check fired. A single failure permanently showed "API is offline" with no recovery.

## Solution

### Cookie read ordering

Moved the cookie read to the root layout **before** `getUser()` runs:

```typescript
// +layout.server.ts (root)
const hadSession = Boolean(cookies.get(REFRESH_TOKEN_COOKIE));
const { user, error: backendError } = await getUser(fetch, url.origin);
return { user, backendError, hadSession, ... };
```

The `(app)` layout reads `hadSession` from `parent()` instead of cookies.

### Global health polling

Replaced the per-component health check with a global reactive state (`$lib/state/health.svelte.ts`):

| State | Poll interval | Rationale |
|-------|--------------|-----------|
| Online | 30s | Light touch, won't burn rate limits |
| Offline | 5s | Fast recovery detection |
| Tab hidden | Paused | No wasted requests |
| Tab visible | Immediate | Instant feedback on return |

`LoginForm` uses `$derived(healthState.online)` — the button updates live.

## Files changed

| File | Change |
|------|--------|
| `src/lib/auth/index.ts` | Added shared `REFRESH_TOKEN_COOKIE` constant |
| `src/routes/+layout.server.ts` | Read cookie before `getUser`, return `hadSession` |
| `src/routes/(app)/+layout.server.ts` | Read `hadSession` from parent, simplified |
| `src/routes/(public)/login/+page.svelte` | Wrapped `replaceState` in `onMount` |
| `src/lib/state/health.svelte.ts` | New global health polling state |
| `src/lib/state/index.ts` | Export health state |
| `src/routes/+layout.svelte` | Initialize health polling |
| `src/lib/components/auth/LoginForm.svelte` | Use global health state |
| `src/routes/(public)/login/+page.server.ts` | Generalized `sessionExpired` boolean to typed `LoginReason` union |
| `src/routes/(public)/login/+page.svelte` | Handle both `session_expired` and `password_changed` toasts |
| `src/lib/components/settings/ChangePasswordForm.svelte` | Hard navigation to `/login?reason=password_changed` |
| `src/messages/en.json`, `cs.json` | Added `auth_passwordChanged_*` i18n messages |

## Review follow-up

| Finding | Fix |
|---------|-----|
| Client-only singleton comment | Added JSDoc warning to `health.svelte.ts` |
| `initHealthCheck` not idempotent | Added `initialized` guard with early return |
| Inconsistent optional chaining on cleanup | Changed `cleanupHealth()` to `cleanupHealth?.()` |

## Tests added

| File | Tests | Covers |
|------|-------|--------|
| `src/routes/layout.server.test.ts` | 5 | Root layout: cookie read timing, hadSession flag |
| `src/routes/(app)/layout.server.test.ts` | 5 | Auth guard: redirect logic with hadSession from parent |
| `src/routes/(public)/login/page.server.test.ts` | 5 | Login page: reason parsing, authenticated redirect, password_changed |
| `src/lib/state/health.test.ts` | 7 | Health polling: state transitions, adaptive intervals, cleanup |
