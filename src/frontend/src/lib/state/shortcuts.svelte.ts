import { IS_MAC } from '$lib/utils/platform';
import * as m from '$lib/paraglide/messages';

// --- State ---

class ShortcutsState {
	isHelpOpen = $state(false);
	isCommandPaletteOpen = $state(false);
}

export const shortcutsState = new ShortcutsState();

// --- Configuration ---

export const ShortcutAction = {
	CommandPalette: 'commandPalette',
	Settings: 'settings',
	Logout: 'logout',
	Help: 'help',
	ToggleSidebar: 'toggleSidebar'
} as const;

export type ShortcutActionType = (typeof ShortcutAction)[keyof typeof ShortcutAction];

export interface ShortcutConfig {
	/** KeyboardEvent.code to match (e.g. "KeyK", "Comma") */
	code: string;
	/** Require platform modifier (Cmd on Mac, Ctrl on others) */
	mod?: boolean;
	/** Require Shift modifier */
	shift?: boolean;
	action: ShortcutActionType;
	description: () => string;
	/** Platform-aware display label */
	display: () => string;
	/** Fire even when an input/textarea is focused */
	allowInInput?: boolean;
}

const SHORTCUTS: ShortcutConfig[] = [
	{
		code: 'KeyK',
		mod: true,
		action: ShortcutAction.CommandPalette,
		description: m.shortcuts_commandPalette,
		display: () => (IS_MAC ? '⌘ K' : 'Ctrl+K'),
		allowInInput: true
	},
	{
		code: 'Comma',
		mod: true,
		action: ShortcutAction.Settings,
		description: m.shortcuts_settings,
		display: () => (IS_MAC ? '⌘ ,' : 'Ctrl+,')
	},
	{
		code: 'KeyL',
		mod: true,
		shift: true,
		action: ShortcutAction.Logout,
		description: m.shortcuts_logout,
		display: () => (IS_MAC ? '⌘ ⇧ L' : 'Ctrl+Shift+L')
	},
	{
		code: 'BracketLeft',
		mod: true,
		action: ShortcutAction.ToggleSidebar,
		description: m.shortcuts_toggleSidebar,
		display: () => (IS_MAC ? '⌘ [' : 'Ctrl+[')
	},
	{
		code: 'Slash',
		shift: true,
		action: ShortcutAction.Help,
		description: m.shortcuts_help,
		display: () => (IS_MAC ? '⇧ ?' : 'Shift+?')
	}
];

export function getAllShortcuts(): ShortcutConfig[] {
	return SHORTCUTS;
}

export function getShortcutSymbol(action: ShortcutActionType): string {
	const config = SHORTCUTS.find((s) => s.action === action);
	return config?.display() ?? '';
}

// --- Action ---

export type ShortcutHandlers = Partial<Record<ShortcutActionType, () => void>>;

function isInput(target: EventTarget | null): boolean {
	if (!(target instanceof HTMLElement)) return false;
	return (
		target.tagName === 'INPUT' ||
		target.tagName === 'TEXTAREA' ||
		target.tagName === 'SELECT' ||
		target.isContentEditable
	);
}

function matchShortcut(event: KeyboardEvent): ShortcutConfig | undefined {
	const mod = IS_MAC ? event.metaKey : event.ctrlKey;

	for (const sc of SHORTCUTS) {
		if (event.code !== sc.code) continue;
		if (sc.mod && !mod) continue;
		if (!sc.mod && mod) continue;
		if (sc.shift && !event.shiftKey) continue;
		if (!sc.shift && event.shiftKey) continue;
		// Block if Alt is pressed (avoid interfering with AltGr combos)
		if (event.altKey) continue;
		return sc;
	}
	return undefined;
}

export function globalShortcuts(node: Window, handlers: ShortcutHandlers = {}) {
	let currentHandlers = handlers;

	function onKeydown(event: KeyboardEvent) {
		const sc = matchShortcut(event);
		if (!sc) return;
		if (!sc.allowInInput && isInput(event.target)) return;
		event.preventDefault();
		executeAction(sc.action, currentHandlers);
	}

	node.addEventListener('keydown', onKeydown, { capture: true });

	return {
		update(newHandlers: ShortcutHandlers) {
			currentHandlers = newHandlers;
		},
		destroy() {
			node.removeEventListener('keydown', onKeydown, { capture: true });
		}
	};
}

function executeAction(action: ShortcutActionType, handlers: ShortcutHandlers) {
	if (action === ShortcutAction.Help) {
		shortcutsState.isHelpOpen = !shortcutsState.isHelpOpen;
		return;
	}
	if (action === ShortcutAction.CommandPalette) {
		shortcutsState.isCommandPaletteOpen = !shortcutsState.isCommandPaletteOpen;
		return;
	}
	handlers[action]?.();
}
