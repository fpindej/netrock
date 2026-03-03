# Full Sweep Cleanup

**Date**: 2026-03-03
**Scope**: Dead code removal, stale documentation fixes, hardcoded color replacements, and em dash elimination across the frontend codebase.

## Summary

Comprehensive cleanup PR following the auth redesign (#416) and auth polish (#417) merges. Removed dead components, unused CSS, stale i18n keys, unused type exports, fixed documentation references, replaced hardcoded colors with semantic tokens, and eliminated em dashes project-wide.

## Changes Made

| File | Change | Reason |
|------|--------|--------|
| `components/common/WorkInProgress.svelte` | Rewritten to use shadcn Alert | Replaced custom Card with glow effects and pulse-ring animation |
| `components/ui/scroll-area/` (3 files) | Deleted | Unused shadcn component - no consumers |
| `styles/utilities.css` | Removed glow-success, glow-destructive, glow-warning | Dead CSS after WorkInProgress rewrite |
| `styles/animations.css` | Removed pulse-ring keyframes and class | Dead CSS after WorkInProgress rewrite |
| `messages/en.json`, `messages/cs.json` | Removed 11 stale keys each | Keys left behind by auth redesign, breadcrumb nav, and component replacements |
| `lib/schemas/auth.ts` | Removed LoginSchema, RegisterSchema type exports | Unused after superforms migration |
| `lib/types/index.ts` | Removed ListUsersResponse, RoleDetail, ListAuditEventsResponse | No consumers found |
| `FILEMAP.md` | SidebarNav -> AppSidebar (2 refs) | Component was renamed during sidebar migration |
| `src/frontend/AGENTS.md` | Fixed layout component list, removed dead sidebar.svelte.ts from state table, updated shortcuts.svelte.ts exports | Stale after shadcn sidebar migration |
| `.claude/skills/new-page/SKILL.md` | SidebarNav -> AppSidebar | Stale reference |
| `.claude/skills/add-permission/SKILL.md` | SidebarNav -> AppSidebar | Stale reference |
| `ui/button/button.svelte` | text-white -> text-destructive-foreground | Hardcoded color |
| `ui/slider/slider.svelte` | bg-white -> bg-background | Hardcoded color |
| `components/profile/ProfileHeader.svelte` | bg-black/50 -> bg-foreground/50, text-white -> text-background | Hardcoded colors |
| `ui/alert-dialog/alert-dialog-overlay.svelte` | bg-black/50 -> bg-foreground/80 | Hardcoded color |
| `ui/dialog/dialog-overlay.svelte` | bg-black/50 -> bg-foreground/80 | Hardcoded color |
| `ui/sheet/sheet-overlay.svelte` | bg-black/50 -> bg-foreground/80 | Hardcoded color |
| 38 files (`.ts`, `.svelte`, `.css`, `.gitignore`) | Replaced em dashes with regular dashes | Project anti-pattern per CLAUDE.md |

## Decisions & Reasoning

### WorkInProgress uses shadcn Alert instead of custom Card

- **Choice**: Replace Card + glow/pulse-ring with a plain shadcn Alert
- **Alternatives considered**: Keep Card with simplified styling, use a custom banner
- **Reasoning**: Alert is the semantic fit for an informational notice. The glow and pulse-ring effects were over-engineered for a placeholder component.

### Overlay opacity changed from /50 to /80

- **Choice**: bg-foreground/80 for dialog overlays
- **Alternatives considered**: Keep /50 with semantic token
- **Reasoning**: Foreground color is lighter than pure black in some themes, so /80 maintains equivalent visual weight while being theme-aware.

### Em dashes removed globally

- **Choice**: Replace all ` - ` (em dash) with ` - ` (regular dash)
- **Alternatives considered**: Rewrite individual sentences
- **Reasoning**: Bulk replacement is safe because all occurrences were comment/documentation separators in ` - ` format. Project rules explicitly prohibit em dashes.

## Follow-Up Items

- [ ] Audit remaining `text-white`/`bg-white`/`bg-black` across the codebase (brand colors in ProviderIcon and QR hex colors were intentionally skipped)
