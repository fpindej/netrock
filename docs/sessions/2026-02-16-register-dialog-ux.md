# Registration Dialog UX Improvements

**Date**: 2026-02-16
**Scope**: Prevent accidental data loss in the registration dialog on the login page

## Summary

The registration dialog could be accidentally closed by clicking the overlay backdrop, losing all filled-in data. Two complementary protections were added: disabling overlay-click dismiss, and persisting non-sensitive draft fields to localStorage so data survives both accidental close and page refresh.

## Changes Made

| File | Change | Reason |
|------|--------|--------|
| `src/frontend/src/lib/components/auth/RegisterDialog.svelte` | Add `interactOutsideBehavior="ignore"` to Dialog.Content | Prevents overlay click from closing the dialog |
| `src/frontend/src/lib/components/auth/RegisterDialog.svelte` | Add localStorage draft persistence (`saveDraft`, `loadDraft`, `clearDraft`) | Preserves form data across close/reopen and page refresh |
| `src/frontend/src/lib/components/auth/RegisterDialog.svelte` | `$effect` auto-saves draft fields while dialog is open | Continuous save without manual triggers |
| `src/frontend/src/lib/components/auth/RegisterDialog.svelte` | `handleOpenChange(true)` calls `loadDraft()` instead of nothing | Restores draft when dialog opens |
| `src/frontend/src/lib/components/auth/RegisterDialog.svelte` | `handleOpenChange(false)` clears only passwords/errors instead of `resetForm()` | Draft fields survive close; passwords always cleared for security |
| `src/frontend/src/lib/components/auth/RegisterDialog.svelte` | Success path: `clearDraft()` → `open = false` → `resetForm()` | Ordering prevents `$effect` from re-saving empty draft |

## Decisions & Reasoning

### Overlay click disabled, Escape key kept

- **Choice**: `interactOutsideBehavior="ignore"` on Content, no change to escape behavior
- **Alternatives considered**: Disabling both overlay and Escape (X-only close)
- **Reasoning**: Overlay clicks are the primary source of accidental close (user clicks imprecisely or out of habit). Escape is a deliberate keypress and important for accessibility (screen reader users, keyboard-only navigation). Keeping Escape respects WCAG expectations for modal dialogs.

### localStorage for draft, never passwords

- **Choice**: Persist `firstName`, `lastName`, `email`, `phoneNumber` only
- **Alternatives considered**: Persisting all fields including passwords; using sessionStorage
- **Reasoning**: Passwords must never be written to localStorage (persists across sessions, readable by any JS on the origin). sessionStorage would work but doesn't survive tab close — localStorage provides broader protection. Email is persisted because it's the most tedious to re-enter and is not a secret (it's the login identifier).

### $effect with saveDraft() signal subscription

- **Choice**: `$effect(() => { if (open) saveDraft(); })` — `saveDraft()` reads the draft fields, creating subscriptions implicitly
- **Alternatives considered**: Explicit `const _ = { firstName, ... }` before `saveDraft()` to show subscriptions; saving on close instead of continuously
- **Reasoning**: Since `saveDraft()` reads the fields inside `JSON.stringify(...)`, Svelte's fine-grained reactivity automatically tracks them — the explicit object creation is redundant. Saving continuously (vs on close) protects against page refresh while the dialog is open. The `if (open)` guard ensures the effect unsubscribes from field changes when the dialog is closed, preventing unnecessary saves.

### Success path ordering: open=false before resetForm()

- **Choice**: `clearDraft()` → `open = false` → `resetForm()` → `onSuccess()`
- **Alternatives considered**: Original order (`clearDraft` → `resetForm` → `open = false`)
- **Reasoning**: `resetForm()` sets draft fields to empty strings, which triggers the `$effect`. If `open` is still `true` at that point, the effect re-saves an empty draft to localStorage, undoing `clearDraft()`. Setting `open = false` first means the `$effect` guard prevents any re-save.

## Follow-Up Items

- [ ] Consider applying the same pattern to `CreateUserDialog` and `CreateRoleDialog` in admin if users report similar friction there
