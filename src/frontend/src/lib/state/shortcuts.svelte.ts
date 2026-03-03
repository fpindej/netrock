import { browser } from '$app/environment';
import { tinykeys } from 'tinykeys';
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
	/** tinykeys key binding string (e.g. "$mod+k", "$mod+Shift+l") */
	binding: string;
	action: ShortcutActionType;
	description: () => string;
	/** Display label for the shortcut (platform-aware) */
	display: () => string;
	/** Allow this shortcut to fire even when an input/textarea is focused */
	allowInInput?: boolean;
}

const SHORTCUTS: ShortcutConfig[] = [
	{
		binding: '$mod+k',
		action: ShortcutAction.CommandPalette,
		description: m.shortcuts_commandPalette,
		display: () => (IS_MAC ? '⌘ K' : 'Ctrl+K'),
		allowInInput: true
	},
	{
		binding: '$mod+Comma',
		action: ShortcutAction.Settings,
		description: m.shortcuts_settings,
		display: () => (IS_MAC ? '⌘ ,' : 'Ctrl+,')
	},
	{
		binding: '$mod+Shift+l',
		action: ShortcutAction.Logout,
		description: m.shortcuts_logout,
		display: () => (IS_MAC ? '⌘ ⇧ L' : 'Ctrl+Shift+L')
	},
	{
		binding: '$mod+BracketLeft',
		action: ShortcutAction.ToggleSidebar,
		description: m.shortcuts_toggleSidebar,
		display: () => (IS_MAC ? '⌘ [' : 'Ctrl+[')
	},
	{
		binding: 'Shift+?',
		action: ShortcutAction.Help,
		description: m.shortcuts_help,
		display: () => (IS_MAC ? '⇧ ?' : 'Shift+?')
	}
];

export function getAllShortcuts(): ShortcutConfig[] {
	return SHORTCUTS;
}

export function getShortcutSymbol(action: ShortcutActionType): string {
	if (!browser) return '';
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

export function globalShortcuts(node: Window, handlers: ShortcutHandlers = {}) {
	let currentHandlers = handlers;

	function createHandler(config: ShortcutConfig) {
		return (event: KeyboardEvent) => {
			if (!config.allowInInput && isInput(event.target)) return;
			event.preventDefault();
			executeAction(config.action, currentHandlers);
		};
	}

	const bindings: Record<string, (event: KeyboardEvent) => void> = {};
	for (const sc of SHORTCUTS) {
		bindings[sc.binding] = createHandler(sc);
	}

	const unsubscribe = tinykeys(window, bindings);

	return {
		update(newHandlers: ShortcutHandlers) {
			currentHandlers = newHandlers;
		},
		destroy() {
			unsubscribe();
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
