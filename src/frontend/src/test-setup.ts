import { vi } from 'vitest';

// Global mocks for SvelteKit $app/* modules.
// These provide sensible defaults that individual tests can override via vi.mocked().
// $env/* modules are NOT mocked here — they vary per test and should be mocked individually.

vi.mock('$app/navigation', () => ({
	goto: vi.fn(),
	invalidateAll: vi.fn(),
	beforeNavigate: vi.fn(),
	afterNavigate: vi.fn(),
	onNavigate: vi.fn(),
	pushState: vi.fn(),
	replaceState: vi.fn()
}));

vi.mock('$app/paths', () => ({
	base: '',
	assets: '',
	resolve: vi.fn((path: string) => path)
}));

vi.mock('$app/environment', () => ({
	browser: true,
	dev: false,
	building: false,
	version: 'test'
}));

vi.mock('$app/state', () => ({
	page: {
		url: new URL('http://localhost'),
		params: {},
		route: { id: '' },
		status: 200,
		error: null,
		data: {},
		state: {},
		form: null
	},
	navigating: null,
	updated: { check: vi.fn() }
}));
