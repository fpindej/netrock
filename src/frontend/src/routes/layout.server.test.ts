/**
 * Tests for the root layout server load — the entry point for all routes.
 *
 * The critical behavior tested here is that `hadSession` is determined from
 * the refresh token cookie BEFORE `getUser()` runs. This matters because
 * SvelteKit's internal fetch mutates the shared cookie store when the backend
 * sends Set-Cookie headers (e.g. clearing the token on failed refresh).
 */
import { describe, expect, it, vi } from 'vitest';

vi.mock('$app/environment', () => ({ dev: false }));
vi.mock('$lib/config/server', () => ({ SERVER_CONFIG: { API_URL: '' } }));

/** Mock getUser — we test its behavior separately in auth.test.ts. */
const mockGetUser = vi.fn();
vi.mock('$lib/auth', async (importOriginal) => {
	const actual = await importOriginal<typeof import('$lib/auth')>();
	return {
		...actual,
		getUser: (...args: unknown[]) => mockGetUser(...args)
	};
});

// Import AFTER mocks are set up
const { load } = await import('./+layout.server');
const { REFRESH_TOKEN_COOKIE } = await import('$lib/auth');

type LoadEvent = Parameters<typeof load>[0];

const MOCK_USER = {
	id: '00000000-0000-0000-0000-000000000001',
	username: 'test@example.com',
	email: 'test@example.com',
	firstName: 'Test',
	lastName: 'User',
	roles: ['User'],
	permissions: [],
	emailConfirmed: true
};

/** Stubs for ServerLoadEvent properties. */
const EVENT_DEFAULTS = {
	fetch: vi.fn() as typeof fetch,
	getClientAddress: () => '127.0.0.1',
	locals: { user: null, locale: 'en' },
	params: {},
	parent: vi.fn().mockResolvedValue({}),
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

function mockLoadEvent(cookieValue?: string) {
	return {
		...EVENT_DEFAULTS,
		url: new URL('http://localhost'),
		cookies: {
			get: vi.fn((name: string) => (name === REFRESH_TOKEN_COOKIE ? cookieValue : undefined)),
			getAll: vi.fn(() => []),
			set: vi.fn(),
			delete: vi.fn(),
			serialize: vi.fn()
		}
	} as LoadEvent;
}

/** Narrows away the `void` branch of the load return type. */
async function loadResult(event: LoadEvent) {
	const result = await load(event);
	if (!result) throw new Error('Expected load to return data');
	return result;
}

describe('root layout server load', () => {
	it('no refresh cookie — hadSession is false', async () => {
		mockGetUser.mockResolvedValueOnce({ user: null, error: null });

		const result = await loadResult(mockLoadEvent());

		expect(result.hadSession).toBe(false);
	});

	it('refresh cookie present — hadSession is true', async () => {
		mockGetUser.mockResolvedValueOnce({ user: null, error: null });

		const result = await loadResult(mockLoadEvent('some-token-value'));

		expect(result.hadSession).toBe(true);
	});

	it('reads the correct cookie name', async () => {
		mockGetUser.mockResolvedValueOnce({ user: null, error: null });
		const event = mockLoadEvent();

		await load(event);

		// Literal string intentional — contract test against the backend's
		// CookieNames.RefreshToken. If the exported constant drifts, this catches it.
		expect(event.cookies.get).toHaveBeenCalledWith('__Secure-REFRESH-TOKEN');
	});

	it('authenticated user — returns user data with hadSession true', async () => {
		mockGetUser.mockResolvedValueOnce({ user: MOCK_USER, error: null });

		const result = await loadResult(mockLoadEvent('token'));

		expect(result.user).toEqual(MOCK_USER);
		expect(result.hadSession).toBe(true);
	});

	it('backend unavailable — returns error with hadSession', async () => {
		mockGetUser.mockResolvedValueOnce({ user: null, error: 'backend_unavailable' });

		const result = await loadResult(mockLoadEvent('token'));

		expect(result.backendError).toBe('backend_unavailable');
		expect(result.hadSession).toBe(true);
	});
});
