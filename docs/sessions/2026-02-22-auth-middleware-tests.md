# Frontend Auth Middleware Tests

**Date**: 2026-02-22
**Scope**: First frontend test file — unit tests for `createAuthMiddleware`

## Summary

Added 11 unit tests covering all code paths of the `createAuthMiddleware()` function from `src/lib/auth/middleware.ts`. This is the first test file in the frontend project. The middleware is pure logic (no DOM, no Svelte, no `$app/*` imports), making it straightforward to test with `vi.fn()` mocks for `fetchFn` and `onAuthFailure`, using real `Request`/`Response` globals.

## Changes Made

| File | Change | Reason |
|------|--------|--------|
| `src/frontend/src/lib/auth/middleware.test.ts` | New — 11 vitest tests | Cover all middleware code paths: pass-through, refresh+retry, refresh failure, deduplication, guard safety |

## Decisions & Reasoning

### Pure-logic middleware as first test target

- **Choice**: Start frontend testing with `createAuthMiddleware` rather than Svelte components or server routes
- **Alternatives considered**: Component tests with `@testing-library/svelte`, end-to-end tests with Playwright
- **Reasoning**: The auth middleware is the highest-value target — it handles token refresh, retry decisions, failure callbacks, and concurrent 401 deduplication. As a pure function with no framework dependencies, it needs only vitest and standard `Request`/`Response` globals, avoiding DOM or Svelte runtime setup entirely.

### Real Request/Response over mocked objects

- **Choice**: Construct real `Request` and `Response` instances in tests
- **Alternatives considered**: Plain objects cast to the types, custom test doubles
- **Reasoning**: Vitest runs in a Node-compatible environment where `Request` and `Response` are globally available. Using real instances ensures the middleware interacts with the same API surface it sees at runtime, catching issues that simplified mocks would hide.

## Follow-Up Items

- [ ] Add component tests for Svelte auth components (`LoginDialog`, `RegisterDialog`) using `@testing-library/svelte`
- [ ] Add tests for `getUser()` in `auth.ts` (structured `GetUserResult` return paths)
- [ ] Add frontend test coverage reporting to CI
