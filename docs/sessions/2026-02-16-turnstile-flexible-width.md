# Fix Turnstile widget width to match form fields

**Date**: 2026-02-16
**Scope**: Make the Cloudflare Turnstile CAPTCHA widget responsive so it matches the width of adjacent form inputs.

## Summary

The Turnstile widget rendered at a fixed ~300px width, visually misaligned with full-width form fields in both the RegisterDialog and ForgotPasswordForm. Switched from Cloudflare's default `normal` size to `flexible` mode, which makes the widget responsive to its container width.

## Changes Made

| File | Change | Reason |
|------|--------|--------|
| `src/frontend/src/lib/components/auth/TurnstileWidget.svelte` | Added `size: 'flexible'` to `turnstile.render()` options; changed container from `flex justify-center` to `w-full` | Widget now stretches to match form field width |

## Decisions & Reasoning

### Use Turnstile `flexible` size mode instead of CSS overrides

- **Choice**: Pass `size: 'flexible'` to the Turnstile render API
- **Alternatives considered**: CSS `!w-full` on the iframe via `[&>iframe]:!w-full`
- **Reasoning**: The Turnstile widget renders inside a cross-origin iframe. CSS overrides can stretch the iframe element itself, but the internal content stays at a fixed width, creating a misaligned layout. Cloudflare's `flexible` size mode (available since late 2024) instructs the widget to render responsively from the inside, properly filling its container. This is the intended API for responsive layouts.

## Follow-Up Items

- [ ] Verify ForgotPasswordForm also renders correctly with the flexible widget
