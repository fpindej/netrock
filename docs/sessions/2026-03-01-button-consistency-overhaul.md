# Button Consistency Overhaul

**Date**: 2026-03-01
**Scope**: Standardize action button sizing, layout, and responsive behavior across admin and settings pages

## Summary

Standardized all action/submit buttons to follow a single responsive pattern: full-width stacked on mobile, auto-width right-aligned on desktop. Fixed the SuperAdmin role card misleadingly showing "No permissions" when it has implicit full access. Documented the button layout convention in `AGENTS.md`.

## Changes Made

| File | Change | Reason |
|------|--------|--------|
| `src/frontend/src/lib/components/admin/RoleCardGrid.svelte` | Show "Implicit full access" for SuperAdmin instead of "No permissions" | SuperAdmin has no explicit permissions array but has full access implicitly |
| `src/frontend/src/lib/components/admin/RoleDetailsCard.svelte` | Wrapped save button in `flex flex-col gap-2 sm:flex-row sm:justify-end`, added `w-full sm:w-auto` | Button was bare/left-aligned, inconsistent with other pages |
| `src/frontend/src/lib/components/admin/RolePermissionsSection.svelte` | Same wrapper pattern for save permissions button | Same issue as RoleDetailsCard |
| `src/frontend/src/lib/components/admin/JobActionsCard.svelte` | Changed from `flex flex-wrap gap-2` to stacked-on-mobile right-aligned-on-desktop layout | Buttons were inline and not responsive |
| `src/frontend/src/lib/components/admin/AccountActions.svelte` | Added `sm:justify-end`, replaced `size="default"` with `class="w-full sm:w-auto"` | Buttons were left-aligned, missing responsive width |
| `src/frontend/src/lib/components/settings/TwoFactorCard.svelte` | Changed from `flex flex-wrap gap-2` to standard pattern | Inconsistent with other settings sections |
| `src/frontend/src/routes/(app)/settings/+page.svelte` | Changed delete button from `shrink-0` to `w-full sm:w-auto` | Consistent responsive behavior |
| `src/frontend/src/messages/en.json` | Added `admin_roles_implicitFullAccess` key | i18n for SuperAdmin label |
| `src/frontend/src/messages/cs.json` | Added Czech translation | i18n parity |
| `src/frontend/AGENTS.md` | Added Button Layout section, updated component listings, added don'ts | Document the convention for future consistency |

## Decisions & Reasoning

### Single button pattern for all action buttons

- **Choice**: `w-full sm:w-auto` on buttons, `flex flex-col gap-2 sm:flex-row sm:justify-end` on wrapper
- **Alternatives considered**: Per-context sizing (small buttons for secondary actions, large for primary), left-aligned buttons
- **Reasoning**: One pattern eliminates decision fatigue and ensures visual consistency across all pages. Mobile-first full-width buttons provide excellent touch targets. Right-alignment on desktop follows the natural reading flow for form actions.

### SuperAdmin label wording

- **Choice**: "Implicit full access" / "Implicitni plny pristup"
- **Alternatives considered**: "All permissions", "Full access", hiding the label entirely
- **Reasoning**: "Implicit full access" communicates both that the role has full access AND that this is implicit (not via explicit permission assignment), which is technically accurate and helps admins understand why no permissions are listed.

## Follow-Up Items

- [ ] Verify visual consistency at 375px, 768px, 1024px, 1440px widths
- [ ] Test RTL layout with logical CSS properties
