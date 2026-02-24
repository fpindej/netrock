import { describe, expect, it, vi } from 'vitest';
import { isRedirect } from '@sveltejs/kit';
import { load } from './+page.server';
import { MOCK_USER, createMockLoadEvent } from '../../../test-utils';

type LoadEvent = Parameters<typeof load>[0];

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

	return createMockLoadEvent({
		route: { id: '/(public)/login' },
		parent: vi.fn().mockResolvedValue({ user }),
		url
	}) as LoadEvent;
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
