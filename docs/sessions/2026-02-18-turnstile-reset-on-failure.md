# Reset Turnstile widget after failed form submission

**Date**: 2026-02-18
**Scope**: Fix stale CAPTCHA token replay on Register and ForgotPassword forms

## Summary

After a failed submission on Register or ForgotPassword, the Cloudflare Turnstile token is consumed server-side (single-use) but the widget doesn't refresh. The user's next submit replays the stale token and gets "CAPTCHA verification failed". This fix resets the widget after any failed submission so a fresh token is generated before retry.

## Changes Made

| File | Change | Reason |
|------|--------|--------|
| `src/frontend/src/lib/components/auth/TurnstileWidget.svelte` | Added `resetRef` callback prop and `reset()` function | Expose a way for parent forms to programmatically reset the widget |
| `src/frontend/src/lib/components/auth/RegisterDialog.svelte` | Store reset ref; call on API error and catch | Clear consumed token so user can retry |
| `src/frontend/src/lib/components/auth/ForgotPasswordForm.svelte` | Store reset ref; call on API error and catch | Same pattern for forgot-password flow |

## Decisions & Reasoning

### Callback ref pattern (`resetRef`) vs imperative handle

- **Choice**: `resetRef` callback that hands the parent a `reset` function
- **Alternatives considered**: Svelte `bind:this` with exported function; reactive `$effect` watching a reset counter prop
- **Reasoning**: Follows the existing callback pattern (`onVerified`, `onError`) already used by the component. No need for `bind:this` coupling or reactive side-effects — the parent calls reset exactly when it needs to, keeping control flow explicit.

### Clearing `captchaToken` alongside widget reset

- **Choice**: Both `resetCaptcha?.()` and `captchaToken = ''` at each failure site
- **Alternatives considered**: Only resetting the widget (relying on `onVerified('')` inside `reset()` to propagate)
- **Reasoning**: The `onVerified('')` call inside `reset()` does clear the token via the parent's callback, but setting `captchaToken = ''` explicitly at the call site makes the intent obvious and guards against any future refactor that might decouple the two.

## Follow-Up Items

- [ ] Verify manually: trigger a registration error (e.g. duplicate email) and confirm widget re-renders, submit button disables until new token arrives
