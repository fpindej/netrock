/**
 * Reactive sidebar state for collapse/expand functionality.
 * Persists preference to localStorage.
 */

const STORAGE_KEY = 'sidebar-collapsed';

function getInitialState(): boolean {
	if (typeof window === 'undefined') return false;
	const stored = localStorage.getItem(STORAGE_KEY);
	return stored === 'true';
}

/**
 * Sidebar state object with reactive collapsed property.
 * Use `sidebarState.collapsed` to read and trigger reactivity.
 */
export const sidebarState = $state({
	collapsed: false
});

/**
 * Initialize sidebar state from localStorage.
 * Call this in onMount to avoid SSR hydration mismatch.
 */
export function initSidebar(): void {
	sidebarState.collapsed = getInitialState();
}

/**
 * Toggle the sidebar collapsed state.
 */
export function toggleSidebar(): void {
	sidebarState.collapsed = !sidebarState.collapsed;
	if (typeof window !== 'undefined') {
		localStorage.setItem(STORAGE_KEY, String(sidebarState.collapsed));
	}
}

/**
 * Set the sidebar collapsed state explicitly.
 */
export function setSidebarCollapsed(value: boolean): void {
	sidebarState.collapsed = value;
	if (typeof window !== 'undefined') {
		localStorage.setItem(STORAGE_KEY, String(sidebarState.collapsed));
	}
}
