# Admin UI Design Improvements

**Date**: 2026-02-16
**Scope**: Dark mode checkbox contrast, button sizing consistency, read-only state indicators, roles page redesign

## Summary

Addressed four design issues in the admin UI: fixed dark mode checkbox contrast that made checked state nearly invisible, standardized button heights across all admin cards, added lock-notice bars with visual muting for read-only permission states, and redesigned the roles list page from a flat table to a card grid that surfaces each role's permissions at a glance.

## Changes Made

| File | Change | Reason |
|------|--------|--------|
| `frontend/.../ui/checkbox/checkbox.svelte` | Override dark mode checked state to `bg-foreground text-background` | `--primary` in dark mode is a warm light gray that blends into the dark card background |
| `frontend/.../admin/UserManagementCard.svelte` | `size="sm" class="min-h-10"` → `size="default"`, added lock notice bar + opacity wrapper | Consistent 40px button height; clear read-only state |
| `frontend/.../admin/JobActionsCard.svelte` | `size="sm"` → `size="default"` on all 4 buttons | Consistent 40px button height across cards |
| `frontend/.../admin/RoleDetailsCard.svelte` | `size="sm"` → `size="default"`, added lock notice bar | Button consistency; read-only state clarity |
| `frontend/.../admin/RolePermissionsSection.svelte` | `size="sm"` → `size="default"`, added lock notice bar | Button consistency; read-only state clarity |
| `frontend/.../admin/RoleDeleteSection.svelte` | `size="sm"` → `size="default"` on delete trigger | Button consistency |
| `frontend/.../admin/RoleCardGrid.svelte` | New component replacing `RoleTable.svelte` | Card grid with permission badges per role |
| `frontend/.../admin/RoleTable.svelte` | Deleted | Replaced by `RoleCardGrid` |
| `frontend/.../admin/index.ts` | Barrel export `RoleTable` → `RoleCardGrid` | Component rename |
| `frontend/.../(app)/admin/roles/+page.svelte` | Use `RoleCardGrid`, remove `Card` wrapper | Cards are self-contained now |
| `frontend/src/messages/en.json` | Added `common_readOnlyNotice`, `admin_roles_noPermissions` | i18n for new UI elements |
| `frontend/src/messages/cs.json` | Czech translations for new messages | i18n parity |
| `backend/.../Dtos/AdminRoleOutput.cs` | Added `IReadOnlyList<string> Permissions` parameter | Surface permissions in role list API |
| `backend/.../Services/AdminService.cs` | Bulk-fetch role claims, pass to `AdminRoleOutput` | Single query for all role permissions (no N+1) |
| `backend/.../Dtos/AdminRoleResponse.cs` | Added `Permissions` property | Expose permissions in API response |
| `backend/.../AdminMapper.cs` | Map `Permissions` field | Complete the DTO chain |
| `backend/tests/.../AdminControllerTests.cs` | Updated test data to include permissions | Fix compilation after record change |
| `frontend/AGENTS.md` | `RoleTable` → `RoleCardGrid` in component tree | Keep docs accurate |

## Decisions & Reasoning

### Checkbox dark mode fix: component-level override vs theme palette change

- **Choice**: Override `dark:data-[state=checked]:bg-foreground` on the checkbox primitive only
- **Alternatives considered**: Changing `--primary` dark mode palette globally
- **Reasoning**: The `--primary` color is used by buttons, badges, and other elements that look correct. Changing it globally would cause regressions. The checkbox's checked state is uniquely problematic because it's a small filled area where low contrast is most noticeable.

### Role claims query: client-side GroupBy vs server-side GroupBy

- **Choice**: Materialize filtered claims with `ToListAsync`, then `GroupBy` in-memory
- **Alternatives considered**: Server-side `GroupBy` + `ToDictionaryAsync` with value selector
- **Reasoning**: The server-side `GroupBy` with a complex value selector (`g.Select(c => c.ClaimValue!).ToList()`) is not supported by all EF Core providers (fails on SQLite in tests). Materializing first is safe and the dataset is small (role claims are a bounded, low-cardinality set).

### Read-only state: opacity wrapper vs individual field styling

- **Choice**: Lock notice bar at top + `opacity-60` wrapper around content area
- **Alternatives considered**: Styling each individual input/button as disabled, adding a full-card overlay
- **Reasoning**: A card-level opacity treatment is immediately obvious at a glance without needing to inspect individual elements. The lock notice bar provides an accessible text explanation. No overlay is needed since `pointer-events-none` isn't applied (existing `disabled` props handle interaction blocking).

### Button sizing: `size="default"` vs custom size variant

- **Choice**: Standardize on `size="default"` (h-10 / 40px) for all card action buttons
- **Alternatives considered**: Creating a custom `size="card"` variant, keeping `size="sm"` with `min-h-10`
- **Reasoning**: `size="default"` is the native 40px height, no hacks needed. Page-level header buttons stay `size="sm"` since they're in a different visual context (header row, not card action area).

## Follow-Up Items

- [ ] Visual QA in browser: verify dark mode checkbox contrast, card grid layout, read-only states
- [ ] Consider adding permission count to the role card footer for quick scanning
