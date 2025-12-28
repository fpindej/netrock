import { browser } from '$app/environment';

// Global state for the help modal
let isHelpOpen = $state(false);

export function getIsHelpOpen() {
	return isHelpOpen;
}

export function toggleHelp() {
	isHelpOpen = !isHelpOpen;
}

export function setHelpOpen(value: boolean) {
	isHelpOpen = value;
}

// --- Configuration ---

export const ShortcutAction = {
	Settings: 'settings',
	Logout: 'logout',
	Help: 'help'
} as const;

export type ShortcutActionType = (typeof ShortcutAction)[keyof typeof ShortcutAction];

interface ShortcutConfig {
	key: string;
	meta?: boolean; // Cmd on Mac, Ctrl on Windows/Linux
	shift?: boolean;
	alt?: boolean;
	ctrl?: boolean; // Explicit Ctrl
	action: ShortcutActionType;
}

// Centralized configuration - easy to modify and read
const SHORTCUTS: ShortcutConfig[] = [
	{ key: ',', meta: true, action: ShortcutAction.Settings },
	{ key: 'l', meta: true, shift: true, action: ShortcutAction.Logout },
	{ key: '?', shift: true, action: ShortcutAction.Help }
];

export function getShortcutSymbol(action: ShortcutActionType): string {
	if (!browser) return '';
	const config = SHORTCUTS.find((s) => s.action === action);
	if (!config) return '';

	const isMac = /Mac|iPod|iPhone|iPad/.test(navigator.userAgent);
	const parts: string[] = [];

	if (config.meta) parts.push(isMac ? '⌘' : 'Ctrl');
	if (config.ctrl) parts.push('Ctrl');
	if (config.alt) parts.push(isMac ? '⌥' : 'Alt');
	if (config.shift) parts.push(isMac ? '⇧' : 'Shift');

	// Capitalize key if it's a letter
	const key = config.key.toUpperCase();
	parts.push(key);

	return parts.join(isMac ? '' : '+');
}

// --- Action ---

// Map actions to handler functions directly
export type ShortcutHandlers = Partial<Record<ShortcutActionType, () => void>>;

export function globalShortcuts(node: Window, handlers: ShortcutHandlers = {}) {
	let currentHandlers = handlers;

	function handleKeydown(event: KeyboardEvent) {
		if (!browser) return;

		// 1. Ignore inputs
		const target = event.target as HTMLElement;
		if (isInput(target)) return;

		// 2. Platform detection
		const isMac = /Mac|iPod|iPhone|iPad/.test(navigator.userAgent);

		// 3. Check against configuration
		for (const sc of SHORTCUTS) {
			// Map abstract modifiers to physical keys based on platform
			const pressedMeta = isMac ? event.metaKey : event.ctrlKey; // The "Command" intent
			const pressedCtrl = isMac ? event.ctrlKey : false; // Explicit Control (rare on Win if mapped to meta)

			if (!!sc.meta !== pressedMeta) continue;
			if (!!sc.ctrl !== pressedCtrl) continue;
			if (!!sc.shift !== event.shiftKey) continue;
			if (!!sc.alt !== event.altKey) continue;

			// Check key (case-insensitive to handle Shift+L vs l)
			if (event.key.toLowerCase() !== sc.key.toLowerCase()) continue;

			// Match found
			event.preventDefault();
			executeAction(sc.action, currentHandlers);
			return;
		}
	}

	window.addEventListener('keydown', handleKeydown);

	return {
		update(newHandlers: ShortcutHandlers) {
			currentHandlers = newHandlers;
		},
		destroy() {
			window.removeEventListener('keydown', handleKeydown);
		}
	};
}

function isInput(target: HTMLElement) {
	return (
		target.tagName === 'INPUT' ||
		target.tagName === 'TEXTAREA' ||
		target.tagName === 'SELECT' ||
		target.isContentEditable
	);
}

function executeAction(action: ShortcutActionType, handlers: ShortcutHandlers) {
	if (action === ShortcutAction.Help) {
		toggleHelp();
		return;
	}
	// Execute the handler if provided
	handlers[action]?.();
}
