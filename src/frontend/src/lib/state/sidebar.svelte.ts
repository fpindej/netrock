/**
 * Reactive sidebar state for collapse/expand functionality.
 * Persists preference to localStorage.
 */

const STORAGE_KEY = 'sidebar-collapsed';

function getStoredState(): boolean {
	if (typeof window === 'undefined') return true;
	const stored = localStorage.getItem(STORAGE_KEY);
	// Default to collapsed if no preference stored
	return stored !== 'false';
}

/**
 * Sidebar state object with reactive collapsed property.
 * Use `sidebarState.collapsed` to read and trigger reactivity.
 * Starts collapsed to avoid jarring expand-then-collapse on refresh.
 */
export const sidebarState = $state({
	collapsed: true
});

/**
 * Initialize sidebar state from localStorage.
 * Call this in onMount to avoid SSR hydration mismatch.
 * If user prefers expanded, animates open smoothly.
 */
export function initSidebar(): void {
	const shouldCollapse = getStoredState();
	if (!shouldCollapse) {
		// Animate to expanded if that's the stored preference
		requestAnimationFrame(() => {
			sidebarState.collapsed = false;
		});
	}
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
