/**
 * Tests for the admin layout server guard — the permission boundary for admin routes.
 *
 * The admin layout requires at least one of: users.view, roles.view, or jobs.view.
 * Users without any of these permissions are redirected to /.
 *
 * Follows the same pattern as the parent (app) layout test.
 */
import { describe, expect, it, vi } from 'vitest';
import { isRedirect } from '@sveltejs/kit';
import { load } from './+layout.server';

type LoadEvent = Parameters<typeof load>[0];

const MOCK_ADMIN_USER = {
	id: '00000000-0000-0000-0000-000000000001',
	username: 'admin@example.com',
	email: 'admin@example.com',
	firstName: 'Admin',
	lastName: 'User',
	roles: ['Admin'],
	permissions: ['users.view', 'roles.view', 'jobs.view'],
	emailConfirmed: true
};

const MOCK_REGULAR_USER = {
	id: '00000000-0000-0000-0000-000000000002',
	username: 'user@example.com',
	email: 'user@example.com',
	firstName: 'Regular',
	lastName: 'User',
	roles: ['User'],
	permissions: [],
	emailConfirmed: true
};

const MOCK_SUPERADMIN_USER = {
	id: '00000000-0000-0000-0000-000000000003',
	username: 'superadmin@example.com',
	email: 'superadmin@example.com',
	firstName: 'Super',
	lastName: 'Admin',
	roles: ['SuperAdmin'],
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
	route: { id: '/(app)/admin' },
	setHeaders: vi.fn(),
	url: new URL('http://localhost/admin'),
	isDataRequest: false,
	isSubRequest: false,
	isRemoteRequest: false,
	tracing: { enabled: false, root: {}, current: {} },
	depends: vi.fn(),
	untrack: <T>(fn: () => T): T => fn()
};

/** Builds a complete mock SvelteKit load event for the admin layout. */
function mockLoadEvent(
	user: typeof MOCK_ADMIN_USER | typeof MOCK_REGULAR_USER | typeof MOCK_SUPERADMIN_USER
) {
	return {
		...EVENT_DEFAULTS,
		parent: vi.fn().mockResolvedValue({ user })
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

describe('admin layout server load', () => {
	// ── Allowed access ──────────────────────────────────────────────

	it('user with all admin permissions — returns user data', async () => {
		const result = await load(mockLoadEvent(MOCK_ADMIN_USER));
		expect(result).toEqual({ user: MOCK_ADMIN_USER });
	});

	it('user with only users.view — returns user data', async () => {
		const user = { ...MOCK_REGULAR_USER, permissions: ['users.view'] };
		const result = await load(mockLoadEvent(user));
		expect(result).toEqual({ user });
	});

	it('user with only roles.view — returns user data', async () => {
		const user = { ...MOCK_REGULAR_USER, permissions: ['roles.view'] };
		const result = await load(mockLoadEvent(user));
		expect(result).toEqual({ user });
	});

	it('user with only jobs.view — returns user data', async () => {
		const user = { ...MOCK_REGULAR_USER, permissions: ['jobs.view'] };
		const result = await load(mockLoadEvent(user));
		expect(result).toEqual({ user });
	});

	it('SuperAdmin without explicit permissions — returns user data (implicit all)', async () => {
		const result = await load(mockLoadEvent(MOCK_SUPERADMIN_USER));
		expect(result).toEqual({ user: MOCK_SUPERADMIN_USER });
	});

	// ── Denied access ───────────────────────────────────────────────

	it('user without admin permissions — redirects to /', async () => {
		await expectRedirect(() => load(mockLoadEvent(MOCK_REGULAR_USER)), 303, '/');
	});

	it('user with unrelated permissions — redirects to /', async () => {
		const user = { ...MOCK_REGULAR_USER, permissions: ['some.other.permission'] };
		await expectRedirect(() => load(mockLoadEvent(user)), 303, '/');
	});
});
