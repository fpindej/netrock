import { describe, expect, it, vi } from 'vitest';
import { isRedirect } from '@sveltejs/kit';
import { load } from './+page.server';

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
	cookies: {
		get: vi.fn(),
		getAll: vi.fn(() => []),
		set: vi.fn(),
		delete: vi.fn(),
		serialize: vi.fn()
	},
	fetch: vi.fn() as typeof fetch,
	getClientAddress: () => '127.0.0.1',
	locals: { user: null, locale: 'en' },
	params: {},
	platform: undefined,
	request: new Request('http://localhost'),
	route: { id: '/(public)/login' },
	setHeaders: vi.fn(),
	isDataRequest: false,
	isSubRequest: false,
	isRemoteRequest: false,
	tracing: { enabled: false, root: {}, current: {} },
	depends: vi.fn(),
	untrack: <T>(fn: () => T): T => fn()
};

/** Builds a complete mock SvelteKit load event for the login page. */
function mockLoadEvent(
	overrides: {
		user?: typeof MOCK_USER | null;
		searchParams?: Record<string, string>;
	} = {}
) {
	const { user = null, searchParams = {} } = overrides;
	const url = new URL('http://localhost/login');

	for (const [key, value] of Object.entries(searchParams)) {
		url.searchParams.set(key, value);
	}

	return {
		...EVENT_DEFAULTS,
		parent: vi.fn().mockResolvedValue({ user }),
		url
	} as LoadEvent;
}

describe('login page server load', () => {
	// ── Already authenticated ───────────────────────────────────────

	it('authenticated user — redirects to /', async () => {
		try {
			await load(mockLoadEvent({ user: MOCK_USER }));
			expect.fail('Expected redirect to be thrown');
		} catch (e) {
			expect(isRedirect(e)).toBe(true);
			if (isRedirect(e)) {
				expect(e.status).toBe(303);
				expect(e.location).toBe('/');
			}
		}
	});

	// ── Reason query param parsing ──────────────────────────────────

	it('no reason param — returns reason: null', async () => {
		const result = await load(mockLoadEvent());
		expect(result).toEqual({ reason: null });
	});

	it('reason=session_expired — returns reason', async () => {
		const result = await load(mockLoadEvent({ searchParams: { reason: 'session_expired' } }));
		expect(result).toEqual({ reason: 'session_expired' });
	});

	it('reason=password_changed — returns reason', async () => {
		const result = await load(mockLoadEvent({ searchParams: { reason: 'password_changed' } }));
		expect(result).toEqual({ reason: 'password_changed' });
	});

	it('unrecognized reason param — returns reason: null', async () => {
		const result = await load(mockLoadEvent({ searchParams: { reason: 'other' } }));
		expect(result).toEqual({ reason: null });
	});
});
