# Auth Polish - Remove Animations, shadcn InputOTP

**Date**: 2026-03-03
**Scope**: Remove custom Svelte transitions from auth pages, migrate OTP inputs to shadcn InputOTP

## Summary

Removed all custom `fly`/`scale` Svelte transitions from authentication components and replaced the custom login success animation with a standard toast + redirect. Migrated manual OTP code inputs in TwoFactorStep and TwoFactorSetupDialog to the shadcn InputOTP component with auto-submit on complete and code clearing on error.

## Changes Made

| File | Change | Reason |
|------|--------|--------|
| `src/frontend/src/lib/components/ui/input-otp/*.svelte` | Added shadcn input-otp component (5 files) | Provides standard, accessible OTP input |
| `src/frontend/src/lib/components/auth/LoginForm.svelte` | Removed all fly/scale transitions, removed custom success checkmark animation, simplified to toast + redirect | Custom animation was problematic inside shadcn layout |
| `src/frontend/src/lib/components/auth/TwoFactorStep.svelte` | Replaced manual Input with InputOTP, added auto-submit on complete, removed fly transition | Standard OTP component with better UX |
| `src/frontend/src/lib/components/settings/TwoFactorSetupDialog.svelte` | Replaced manual Input with InputOTP, added auto-submit on complete | Consistent OTP experience across app |
| `src/frontend/src/lib/components/auth/ForgotPasswordForm.svelte` | Removed fly/scale transitions and wrapper divs | Consistency - no custom animations in auth pages |
| `src/frontend/src/lib/components/auth/ResetPasswordForm.svelte` | Removed fly/scale transitions and wrapper divs from all 5 conditional states | Consistency - no custom animations in auth pages |

## Decisions & Reasoning

### Remove all Svelte transitions from auth pages

- **Choice**: Strip all `fly`, `scale`, and `fade` transition directives completely
- **Alternatives considered**: Replace with CSS transitions, use lighter Svelte transitions
- **Reasoning**: The custom transitions were causing visual glitches inside the new shadcn AuthShell layout. Removing them entirely gives a clean, instant render that matches the rest of the shadcn-based UI. CSS `animate-shake` (Tailwind) is kept for error feedback since it works well.

### Toast + redirect instead of success animation

- **Choice**: Replace the custom checkmark circle with scale animation and delay with `toast.success()` + immediate `invalidateAll()` + `goto('/')`
- **Alternatives considered**: Keep a simpler success state with just a checkmark (no animation), use a brief fade
- **Reasoning**: The success animation added unnecessary delay before redirect and was visually jarring inside the split-panel layout. A toast notification is the standard pattern used elsewhere in the app and provides immediate feedback without blocking navigation.

### Auto-submit on OTP complete

- **Choice**: OTP input auto-submits when all 6 digits are entered
- **Alternatives considered**: Keep manual submit button only
- **Reasoning**: Auto-submit is the industry standard UX for 6-digit OTP codes. The submit button is kept as a fallback. Code is cleared on error so the user can immediately re-enter.

## Follow-Up Items

- [ ] Evaluate if `FieldError.svelte` can be fully replaced by formsnap `Form.FieldErrors` after superforms migration (PR 2-4 of the plan)
