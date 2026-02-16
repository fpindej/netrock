# Frontend Review and Cleanup

**Date**: 2026-02-16
**Scope**: Comprehensive frontend review covering API client, folder structure, pages/styling, responsive design, Tailwind config, i18n, state management, forms, and TS/SvelteKit best practices — followed by cleanup and form improvements.

## Summary

Conducted a thorough frontend review across 9 dimensions. The codebase scored excellent on API client usage, folder structure, responsive design, Tailwind configuration, state management, and TS/SvelteKit best practices. Cleanup work addressed the findings across two stacked PRs: (#186) removed unused i18n keys, consolidated duplicates, extracted the oversized role detail page into sub-components, and deleted the GettingStarted boilerplate; (#187) extracted a shared `handleMutationError()` helper to eliminate copy-pasted rate-limit handling across 16 components, and added field-level validation to role forms.

## Changes Made

| File | Change | Reason |
|------|--------|--------|
| `src/messages/en.json`, `cs.json` | Removed 6 unused keys | Keys defined but never referenced (accountActions, forgotPassword_success, resetPassword_success, adminRoleDetail meta) |
| `src/messages/en.json`, `cs.json` | Consolidated 6 duplicate keys into `common_cancel`, `common_backToLogin`, `common_dangerZone` | Reduce maintainability burden of identical translations in different contexts |
| `src/lib/components/admin/RoleDetailsCard.svelte` | Created | Extracted from roles/[id] page — role name/description editing with save |
| `src/lib/components/admin/RolePermissionsSection.svelte` | Created | Extracted from roles/[id] page — permission checkbox grid with save |
| `src/lib/components/admin/RoleDeleteSection.svelte` | Created | Extracted from roles/[id] page — danger zone with delete confirmation |
| `src/lib/state/cooldown.svelte.ts` | Added `Cooldown` type export | Enable typed cooldown props in extracted components |
| `src/routes/(app)/admin/roles/[id]/+page.svelte` | Reduced from 255 to ~75 lines | Uses new sub-components; page is now a thin orchestrator |
| `src/lib/components/getting-started/` | Deleted folder | Boilerplate onboarding scaffold designed for removal |
| `src/routes/(app)/analytics/+page.svelte` | Deleted | Placeholder route with no real functionality |
| `src/routes/(app)/reports/+page.svelte` | Deleted | Placeholder route with no real functionality |
| `src/lib/components/layout/SidebarNav.svelte` | Removed Analytics/Reports nav items and unused icons | Routes no longer exist |
| `src/routes/(app)/+page.svelte` | Replaced GettingStarted with clean dashboard skeleton | Remove template boilerplate |
| `src/frontend/AGENTS.md` | Updated directory trees | Reflect file additions and removals |

### PR #187 — Form Improvements (stacked on #186)

| File | Change | Reason |
|------|--------|--------|
| `src/lib/api/mutation.ts` | Created `handleMutationError()` helper | Centralizes rate-limit toast+cooldown pattern from 16 components into one function |
| `src/lib/api/index.ts` | Added mutation.ts re-export | Barrel export for new module |
| 16 form components | Replaced inline rate-limit handling with `handleMutationError()` | Eliminates copy-paste, guarantees consistent behavior |
| `src/lib/components/admin/JobActionsCard.svelte` | Removed local `handleRateLimited()` function | Replaced by shared helper |
| `src/lib/components/admin/UserManagementCard.svelte` | Removed local `handleRateLimited()` function | Replaced by shared helper |
| `src/lib/components/admin/CreateRoleDialog.svelte` | Added fieldErrors, fieldShakes, inline error display | Backend validation errors now appear below the relevant field |
| `src/lib/components/admin/RoleDetailsCard.svelte` | Added fieldErrors, fieldShakes, inline error display | Backend validation errors now appear below the relevant field |

## Decisions & Reasoning

### Shared cooldown across role sub-components

- **Choice**: Pass a single `createCooldown()` instance from the parent page to all three sub-components
- **Alternatives considered**: Each component creates its own cooldown; pass `onRateLimited` callback from parent
- **Reasoning**: Preserves original behavior where a rate limit on any operation disables all buttons. Single source of truth avoids inconsistent countdown states.

### Cooldown type export strategy

- **Choice**: Export `type Cooldown = ReturnType<typeof createCooldown>` from the state module
- **Alternatives considered**: Inline structural type in each component's Props interface; import `createCooldown` function for typeof
- **Reasoning**: Single type definition, avoids repeating the structural type across 3 components, and avoids runtime import issues with `verbatimModuleSyntax`

### handleMutationError API design

- **Choice**: Single function with `onRateLimited`, `onValidationError`, and `onError` optional callbacks
- **Alternatives considered**: Return discriminated union for caller to switch on; separate `handleRateLimitError()` for just rate limiting; put logic in `error-handling.ts`
- **Reasoning**: Rate-limit handling is 100% identical across all 16 forms (8 lines each) — fully automatic. Validation and generic errors vary enough to need callbacks. Separate file (`mutation.ts`) keeps `error-handling.ts` pure (no UI imports like toast/messages).

### LoginForm field-level errors (skipped)

- **Choice**: Keep card-level shake instead of adding per-field validation
- **Alternatives considered**: Add fieldErrors/fieldShakes like other forms
- **Reasoning**: Login endpoints intentionally don't reveal which field was wrong (account enumeration risk). Card-level shake + toast is the correct security-aware UX pattern.

### Debounce utility (skipped)

- **Choice**: Keep inline setTimeout debounce in users search page
- **Alternatives considered**: Extract to `$lib/utils/debounce.ts`
- **Reasoning**: Only one debounce usage in the entire codebase. Extracting a utility for a single call site is premature abstraction.

### GettingStarted deletion scope

- **Choice**: Delete component, placeholder routes, 62+ i18n keys, and sidebar entries all at once
- **Alternatives considered**: Keep WorkInProgress component for future use; convert to a real dashboard
- **Reasoning**: The component was self-documenting boilerplate (contained its own removal instructions). Keeping it adds friction for template consumers who would need to clean it up themselves.

## Review Findings (Non-Actioned)

These areas scored excellent and required no changes:

- **API client**: Zero bypasses, zero type duplication, consistent error handling across all 34+ call sites
- **Folder structure**: All barrel files present, clean feature separation, 100% Svelte 5 runes compliance
- **Responsive design**: Mobile-first dual-layout tables, proper breakpoint usage (sm/md/lg/xl), `motion-safe:` animations, 40px touch targets
- **Tailwind v4 config**: Modular CSS architecture, HSL theme tokens, `prefers-reduced-motion` support, logical CSS properties throughout
- **Accessibility**: Keyboard navigation, ARIA attributes, semantic HTML, focus-visible states
- **State management**: Minimal and correct — `$state`/`$derived` for local, `+page.server.ts` + `invalidateAll()` for server, factory functions for reusable state
- **TS/JS/Vite/SvelteKit**: Strict mode, no `any` types, proper `verbatimModuleSyntax` compliance, correct Vite config, proper SvelteKit data loading

## Follow-Up Items

- [ ] Build a real dashboard to replace the empty skeleton
- [ ] Consider client-side caching (SWR pattern) if API calls become frequent
- [ ] Consider request timeout handling via AbortController
