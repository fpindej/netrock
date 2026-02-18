# Reset Password: Warn Logged-In Users

**Date**: 2026-02-18
**Scope**: Frontend UX fix for logged-in users visiting `/reset-password` via email links

## Summary

Replaced the silent redirect-to-dashboard behavior with an informative warning card when a logged-in user visits `/reset-password`. The card shows the user's email, offers a "Sign out and continue" button (which preserves the token in the URL), and a "Go to dashboard" fallback. Fully i18n-aware with invitation-specific copy variants.

## Changes Made

| File | Change | Reason |
|------|--------|--------|
| `src/frontend/src/routes/(public)/reset-password/+page.server.ts` | Removed `redirect(303, '/')` for logged-in users; now returns `user` in page data | Allow the page to render a warning card instead of silently redirecting |
| `src/frontend/src/routes/(public)/reset-password/+page.svelte` | Pass `user={data.user}` to `ResetPasswordForm` | Thread user data through to the component |
| `src/frontend/src/lib/components/auth/ResetPasswordForm.svelte` | Added `user` prop, `signOutAndContinue` handler, and warning card UI branch | Display contextual warning with sign-out flow that preserves the reset token |
| `src/frontend/src/messages/en.json` | Added 7 i18n keys for warning card | English translations for the new UI states |
| `src/frontend/src/messages/cs.json` | Added 7 i18n keys for warning card | Czech translations for the new UI states |

## Decisions & Reasoning

### Skip `goto()` after logout — use `invalidateAll()` only

- **Choice**: The `signOutAndContinue` handler calls `invalidateAll()` without navigating, unlike the global `logout()` which does `goto('/login')`
- **Alternatives considered**: Reusing `logout()` from `$lib/auth` and re-navigating back; using a full page reload
- **Reasoning**: `invalidateAll()` re-runs the layout and page loads. The root layout sets `user: null` (cookies cleared), the page load returns `user: null`, and the reactive prop update causes the `{#if user}` branch to collapse — revealing the form. The token stays in the URL because no navigation occurred. This is the simplest and most reliable approach.

### Warning card before `isMissingParams` check

- **Choice**: The `{#if user}` branch comes first, before the `isMissingParams` check
- **Alternatives considered**: Checking `isMissingParams` first and showing a combined "signed in + invalid link" state
- **Reasoning**: A logged-in user clicking a valid reset link needs to sign out first. After signing out, the `isMissingParams` check kicks in naturally. If the user visits `/reset-password` (no token) while logged in, they see the warning, sign out, then see the "invalid link" card — which is the correct flow.

## Diagrams

```mermaid
stateDiagram-v2
    [*] --> CheckUser: Page loads
    CheckUser --> WarningCard: user !== null
    CheckUser --> CheckToken: user === null

    WarningCard --> SignOut: "Sign out and continue"
    WarningCard --> Dashboard: "Go to dashboard"
    SignOut --> InvalidateAll: POST /api/auth/logout
    InvalidateAll --> CheckToken: invalidateAll() re-runs loads

    CheckToken --> InvalidLink: no token
    CheckToken --> Form: token present
    Form --> Success: submit OK
    Form --> Error: submit failed
```

## Follow-Up Items

- [ ] Manual test: log in, visit `/reset-password?token=test`, verify warning card
- [ ] Manual test: click "Sign out and continue", verify form appears with token preserved
- [ ] Manual test: verify invitation-aware copy with `&invited` param
- [ ] Manual test: click "Go to dashboard", verify navigation to `/`
