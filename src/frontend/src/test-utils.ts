/**
 * Shared test utilities for SvelteKit server load function tests.
 *
 * Provides a mock user constant and a factory for building mock ServerLoadEvent
 * objects, eliminating duplication across route-level test files.
 */
import { vi } from 'vitest';

/** A realistic user object matching the shape returned by getUser(). */
export const MOCK_USER = {
	id: '00000000-0000-0000-0000-000000000001',
	username: 'test@example.com',
	email: 'test@example.com',
	firstName: 'Test',
	lastName: 'User',
	roles: ['User'],
	permissions: [],
	emailConfirmed: true
} as const;

/**
 * Default stubs for ServerLoadEvent properties that most load functions ignore.
 *
 * Individual tests override specific properties via `createMockLoadEvent()`.
 * Note: `cookies`, `parent`, and `url` are intentionally omitted here because
 * they vary per test and should always be provided explicitly.
 */
const EVENT_DEFAULTS = {
	fetch: vi.fn() as typeof fetch,
	getClientAddress: () => '127.0.0.1',
	locals: { user: null, locale: 'en' },
	params: {},
	platform: undefined,
	request: new Request('http://localhost'),
	route: { id: '/' },
	setHeaders: vi.fn(),
	isDataRequest: false,
	isSubRequest: false,
	isRemoteRequest: false,
	tracing: { enabled: false, root: {}, current: {} },
	depends: vi.fn(),
	untrack: <T>(fn: () => T): T => fn()
};

/** Creates a mock cookies object with vi.fn() stubs for all methods. */
export function createMockCookies(getFn?: (name: string) => string | undefined) {
	return {
		get: vi.fn(getFn ?? (() => undefined)),
		getAll: vi.fn(() => []),
		set: vi.fn(),
		delete: vi.fn(),
		serialize: vi.fn()
	};
}

/**
 * Builds a mock SvelteKit ServerLoadEvent with sensible defaults.
 *
 * Every property can be overridden. Properties not provided fall back to
 * EVENT_DEFAULTS, with `url` defaulting to `http://localhost` and `cookies`
 * defaulting to a no-op mock set, and `parent` defaulting to resolving `{}`.
 *
 * @example
 * ```ts
 * const event = createMockLoadEvent({
 *   parent: vi.fn().mockResolvedValue({ user: MOCK_USER }),
 *   url: new URL('http://localhost/login?reason=session_expired')
 * });
 * ```
 */
export function createMockLoadEvent(overrides: Record<string, unknown> = {}) {
	return {
		...EVENT_DEFAULTS,
		url: new URL('http://localhost'),
		cookies: createMockCookies(),
		parent: vi.fn().mockResolvedValue({}),
		...overrides
	};
}
