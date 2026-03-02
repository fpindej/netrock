import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('$lib/api', () => ({
	browserClient: {
		POST: vi.fn()
	}
}));

vi.mock('$lib/components/ui/sonner', () => ({
	toast: {
		error: vi.fn()
	}
}));

vi.mock('$lib/paraglide/messages', () => ({
	oauth_challengeError: vi.fn(() => 'Challenge error')
}));

import { browserClient } from '$lib/api';
import { toast } from '$lib/components/ui/sonner';
import { startOAuthChallenge } from './oauth';

const mockPost = vi.mocked(browserClient.POST);

/** Stub window.location for Node test environment. */
function stubLocation(): { href: string } {
	const loc = { origin: 'https://app.test', href: '' };
	(globalThis as Record<string, unknown>).window = { location: loc };
	return loc;
}

describe('startOAuthChallenge', () => {
	let loc: { href: string };

	beforeEach(() => {
		vi.clearAllMocks();
		loc = stubLocation();
	});

	afterEach(() => {
		delete (globalThis as Record<string, unknown>).window;
	});

	it('returns true and redirects on successful challenge', async () => {
		const authUrl = 'https://accounts.google.com/o/oauth2/v2/auth?state=abc';
		mockPost.mockResolvedValue({
			response: new Response(null, { status: 200 }),
			data: { authorizationUrl: authUrl }
		} as never);

		const result = await startOAuthChallenge('Google');

		expect(result).toBe(true);
		expect(loc.href).toBe(authUrl);
		expect(toast.error).not.toHaveBeenCalled();
		expect(mockPost).toHaveBeenCalledWith('/api/auth/external/challenge', {
			body: { provider: 'Google', redirectUri: 'https://app.test/oauth/callback' }
		});
	});

	it('returns false and shows toast on non-OK response', async () => {
		mockPost.mockResolvedValue({
			response: new Response(null, { status: 400 }),
			data: undefined
		} as never);

		const result = await startOAuthChallenge('GitHub');

		expect(result).toBe(false);
		expect(toast.error).toHaveBeenCalledWith('Challenge error');
		expect(loc.href).toBe('');
	});

	it('returns false and shows toast on network error', async () => {
		mockPost.mockRejectedValue(new TypeError('Failed to fetch'));

		const result = await startOAuthChallenge('Google');

		expect(result).toBe(false);
		expect(toast.error).toHaveBeenCalledWith('Challenge error');
		expect(loc.href).toBe('');
	});

	it('returns false when response is OK but authorizationUrl is missing', async () => {
		mockPost.mockResolvedValue({
			response: new Response(null, { status: 200 }),
			data: {}
		} as never);

		const result = await startOAuthChallenge('Google');

		expect(result).toBe(false);
		expect(toast.error).toHaveBeenCalledWith('Challenge error');
	});
});
