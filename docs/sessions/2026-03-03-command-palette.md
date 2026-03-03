# Command Palette and Keyboard Shortcuts

**Date**: 2026-03-03
**Scope**: Add Cmd+K command palette, rewrite keyboard shortcuts system, add discoverability

## Summary

Added a command palette (Cmd+K) using shadcn-svelte's Command component with permission-gated navigation, admin items, and quick actions. Replaced the tinykeys library with a direct keydown handler using `event.code` matching for reliable cross-platform shortcuts. Added discoverability hints in the sidebar and mobile header.

## Changes Made

| File | Change | Reason |
|------|--------|--------|
| `src/frontend/src/lib/components/layout/CommandPalette.svelte` | New command palette component | Central navigation and actions via Cmd+K |
| `src/frontend/src/lib/state/shortcuts.svelte.ts` | Rewrote shortcuts system | Replaced tinykeys with direct `event.code` handler for reliability |
| `src/frontend/src/lib/components/layout/AppSidebar.svelte` | Added search trigger with Cmd+K badge | Command palette discoverability |
| `src/frontend/src/lib/components/layout/Header.svelte` | Added search icon button | Mobile discoverability |
| `src/frontend/src/lib/components/layout/ShortcutsHelp.svelte` | Updated to use `display()` method | Align with new ShortcutConfig shape |
| `src/frontend/src/lib/components/layout/index.ts` | Added CommandPalette export | Barrel file |
| `src/frontend/src/routes/(app)/+layout.svelte` | Render CommandPalette | Wire into authenticated layout |
| `src/frontend/src/lib/components/ui/command/` | New shadcn command components | UI primitives for command palette |
| `src/frontend/src/messages/en.json` | Added 8 i18n keys | Command palette and shortcut labels |
| `src/frontend/src/messages/cs.json` | Added 8 i18n keys | Czech translations |
| `FILEMAP.md` | Added CommandPalette entries | Change impact tracking |
| `.claude/skills/new-page/SKILL.md` | Added step for command palette entry | Ensure new pages are also added to command palette |
| `.claude/skills/new-feature/SKILL.md` | Updated step 16 | Same |

## Decisions & Reasoning

### Dropped tinykeys in favor of direct keydown handler

- **Choice**: Custom ~40-line handler matching `event.code`
- **Alternatives considered**: tinykeys v3, hotkeys-js, mousetrap
- **Reasoning**: tinykeys matched letter keys via `event.key` which was unreliable on macOS. The library also lacked TypeScript types in v3 exports (required a manual .d.ts shim). A direct handler using `event.code` (physical key position) with capture phase is simpler, fully typed, and more reliable. The implementation is small enough that a library adds no value.

### event.code over event.key

- **Choice**: Match against `KeyboardEvent.code` (e.g. `KeyK`, `Comma`, `Slash`)
- **Alternatives considered**: `event.key` matching
- **Reasoning**: `event.code` represents the physical key position, making it independent of keyboard layout, input language, and modifier key interactions. The working shortcuts (Comma, BracketLeft) already used code-style names; the failing ones (k, l) used key-style names. Standardizing on `event.code` fixed the issue.

### Permission-gated admin items in command palette

- **Choice**: Filter admin items with `hasPermission()` via `$derived`, same as sidebar
- **Alternatives considered**: Show all items and let server-side guards handle unauthorized access
- **Reasoning**: Consistent with sidebar behavior. Don't reveal admin page existence to users without permissions. Navigation items stay in sync because both components use the same `Permissions.*` constants.

### Cmd+Shift+L conflict with 1Password

- **Choice**: Keep the shortcut as-is, document the conflict
- **Reasoning**: The shortcut works when 1Password isn't intercepting it. Users can remap the 1Password hotkey. Changing our shortcut would surprise users who don't use 1Password. This can be revisited if multiple users report the conflict.

## Follow-Up Items

- [ ] Consider changing Cmd+Shift+L logout shortcut if 1Password conflict is common
- [ ] Add more actions to command palette as features are added (theme selector, language selector)
