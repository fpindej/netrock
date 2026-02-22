import { describe, expect, it, vi } from 'vitest';
import { isHttpError, isRedirect } from '@sveltejs/kit';
import { load } from './+layout.server';

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

/** Stubs for all `ServerLoadEvent` properties the load function does NOT use. */
const EVENT_DEFAULTS = {
	fetch: vi.fn() as typeof fetch,
	getClientAddress: () => '127.0.0.1',
	locals: { user: null, locale: 'en' },
	params: {},
	platform: undefined,
	request: new Request('http://localhost'),
	route: { id: '/(app)' },
	setHeaders: vi.fn(),
	url: new URL('http://localhost'),
	isDataRequest: false,
	isSubRequest: false,
	isRemoteRequest: false,
	tracing: { enabled: false, root: {}, current: {} },
	depends: vi.fn(),
	untrack: <T>(fn: () => T): T => fn()
};

/** Builds a complete mock SvelteKit load event for the (app) layout. */
function mockLoadEvent(
	overrides: {
		user?: typeof MOCK_USER | null;
		backendError?: 'backend_unavailable' | null;
		refreshCookie?: string;
	} = {}
) {
	const { user = null, backendError = null, refreshCookie } = overrides;

	return {
		...EVENT_DEFAULTS,
		parent: vi.fn().mockResolvedValue({ user, backendError }),
		cookies: {
			get: vi.fn((name: string) => (name === '__Secure-REFRESH-TOKEN' ? refreshCookie : undefined)),
			getAll: vi.fn(() => []),
			set: vi.fn(),
			delete: vi.fn(),
			serialize: vi.fn()
		}
	} as LoadEvent;
}

/** Asserts that a load function throws a SvelteKit redirect. */
async function expectRedirect(fn: () => ReturnType<typeof load>, status: number, location: string) {
	try {
		await fn();
		expect.fail('Expected redirect to be thrown');
	} catch (e) {
		expect(isRedirect(e)).toBe(true);
		if (isRedirect(e)) {
			expect(e.status).toBe(status);
			expect(e.location).toBe(location);
		}
	}
}

describe('(app) layout server load', () => {
	// ── Authenticated ───────────────────────────────────────────────

	it('authenticated user — returns user data', async () => {
		const result = await load(mockLoadEvent({ user: MOCK_USER }));
		expect(result).toEqual({ user: MOCK_USER });
	});

	// ── Backend unavailable ─────────────────────────────────────────

	it('backend unavailable — throws 503 error', async () => {
		try {
			await load(mockLoadEvent({ backendError: 'backend_unavailable' }));
			expect.fail('Expected error to be thrown');
		} catch (e) {
			expect(isHttpError(e, 503)).toBe(true);
		}
	});

	it('backend unavailable takes precedence over missing user', async () => {
		try {
			await load(
				mockLoadEvent({ backendError: 'backend_unavailable', refreshCookie: 'some-token' })
			);
			expect.fail('Expected error to be thrown');
		} catch (e) {
			// Should throw 503, not redirect — backend error is checked first
			expect(isHttpError(e, 503)).toBe(true);
			expect(isRedirect(e)).toBe(false);
		}
	});

	// ── Session expired detection ───────────────────────────────────

	it('no user, no refresh cookie — redirects to /login', async () => {
		await expectRedirect(() => load(mockLoadEvent()), 303, '/login');
	});

	it('no user, refresh cookie present — redirects with session_expired reason', async () => {
		await expectRedirect(
			() => load(mockLoadEvent({ refreshCookie: 'some-token-value' })),
			303,
			'/login?reason=session_expired'
		);
	});

	it('reads the correct cookie name', async () => {
		const event = mockLoadEvent();

		try {
			await load(event);
		} catch {
			// redirect is expected
		}

		expect(vi.mocked(event.cookies.get)).toHaveBeenCalledWith('__Secure-REFRESH-TOKEN');
	});
});
