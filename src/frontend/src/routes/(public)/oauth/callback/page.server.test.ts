import { describe, expect, it, vi } from 'vitest';
import { isRedirect } from '@sveltejs/kit';
import { load } from './+page.server';
import { createMockLoadEvent } from '../../../../test-utils';

type LoadEvent = Parameters<typeof load>[0];

function mockLoadEvent(
	overrides: {
		searchParams?: Record<string, string>;
		fetchResponse?: { ok: boolean; json?: unknown };
	} = {}
) {
	const { searchParams = {}, fetchResponse } = overrides;
	const url = new URL('http://localhost/oauth/callback');

	for (const [key, value] of Object.entries(searchParams)) {
		url.searchParams.set(key, value);
	}

	const fetchFn = fetchResponse
		? vi.fn().mockResolvedValue({
				ok: fetchResponse.ok,
				json: vi.fn().mockResolvedValue(fetchResponse.json ?? {})
			})
		: vi.fn().mockRejectedValue(new Error('Network error'));

	return createMockLoadEvent({
		route: { id: '/(public)/oauth/callback' },
		url,
		fetch: fetchFn
	}) as LoadEvent;
}

describe('OAuth callback page server load', () => {
	// ── Error param from provider ──────────────────────────────

	it('error param present - returns provider_denied', async () => {
		const result = await load(mockLoadEvent({ searchParams: { error: 'access_denied' } }));
		expect(result).toEqual({ error: 'provider_denied' });
	});

	it('error param with code - still returns provider_denied', async () => {
		const result = await load(
			mockLoadEvent({
				searchParams: { error: 'access_denied', code: 'some-code', state: 'some-state' }
			})
		);
		expect(result).toEqual({ error: 'provider_denied' });
	});

	// ── Missing params ─────────────────────────────────────────

	it('missing code - returns missing_params', async () => {
		const result = await load(mockLoadEvent({ searchParams: { state: 'some-state' } }));
		expect(result).toEqual({ error: 'missing_params' });
	});

	it('missing state - returns missing_params', async () => {
		const result = await load(mockLoadEvent({ searchParams: { code: 'some-code' } }));
		expect(result).toEqual({ error: 'missing_params' });
	});

	it('no params at all - returns missing_params', async () => {
		const result = await load(mockLoadEvent());
		expect(result).toEqual({ error: 'missing_params' });
	});

	// ── Successful login ───────────────────────────────────────

	it('successful login - redirects to /dashboard', async () => {
		try {
			await load(
				mockLoadEvent({
					searchParams: { code: 'auth-code', state: 'state-token' },
					fetchResponse: {
						ok: true,
						json: { accessToken: 'token', refreshToken: 'refresh', isLinkOnly: false }
					}
				})
			);
			expect.fail('Expected redirect to be thrown');
		} catch (e) {
			expect(isRedirect(e)).toBe(true);
			if (isRedirect(e)) {
				expect(e.status).toBe(303);
				expect(e.location).toBe('/dashboard');
			}
		}
	});

	// ── Link-only (account linking from settings) ──────────────

	it('link-only callback - redirects to /settings', async () => {
		try {
			await load(
				mockLoadEvent({
					searchParams: { code: 'auth-code', state: 'state-token' },
					fetchResponse: { ok: true, json: { isLinkOnly: true } }
				})
			);
			expect.fail('Expected redirect to be thrown');
		} catch (e) {
			expect(isRedirect(e)).toBe(true);
			if (isRedirect(e)) {
				expect(e.status).toBe(303);
				expect(e.location).toBe('/settings');
			}
		}
	});

	// ── API error responses ────────────────────────────────────

	it('API returns error with detail - returns detail message', async () => {
		const result = await load(
			mockLoadEvent({
				searchParams: { code: 'auth-code', state: 'state-token' },
				fetchResponse: {
					ok: false,
					json: { detail: 'Invalid or missing OAuth state token.' }
				}
			})
		);
		expect(result).toEqual({ error: 'Invalid or missing OAuth state token.' });
	});

	it('API returns error without detail - returns Unknown error', async () => {
		const result = await load(
			mockLoadEvent({
				searchParams: { code: 'auth-code', state: 'state-token' },
				fetchResponse: { ok: false, json: {} }
			})
		);
		expect(result).toEqual({ error: 'Unknown error' });
	});

	// ── Network error ──────────────────────────────────────────

	it('network error - returns network_error', async () => {
		const result = await load(
			mockLoadEvent({
				searchParams: { code: 'auth-code', state: 'state-token' }
				// No fetchResponse = fetch rejects with network error
			})
		);
		expect(result).toEqual({ error: 'network_error' });
	});
});
