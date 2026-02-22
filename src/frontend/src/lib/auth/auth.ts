import { goto, invalidateAll } from '$app/navigation';
import { resolve } from '$app/paths';
import { browserClient, createApiClient } from '$lib/api';
import type { User } from '$lib/types';
import { createAuthMiddleware } from './middleware';

/**
 * Result of a server-side user fetch. Distinguishes between "not authenticated"
 * (user is null, no error) and "backend unavailable" (user is null, error is set).
 */
export interface GetUserResult {
	user: User | null;
	error: 'backend_unavailable' | null;
}

/**
 * Fetches the current authenticated user from the backend.
 *
 * Wires `createAuthMiddleware` so that expired access tokens are transparently
 * refreshed via cookies — works on both server-side (SvelteKit load) and
 * browser-side fetch. No `onAuthFailure` callback on server side; if refresh
 * fails, the 401 passes through and is handled as "not authenticated".
 *
 * Returns a structured result so callers can distinguish between:
 * - Authenticated user → `{ user, error: null }`
 * - Not authenticated (401) → `{ user: null, error: null }`
 * - Backend unreachable (5xx / network) → `{ user: null, error: 'backend_unavailable' }`
 */
export async function getUser(
	fetch: typeof globalThis.fetch,
	origin: string
): Promise<GetUserResult> {
	const client = createApiClient(fetch, origin, [createAuthMiddleware(fetch, origin)]);

	try {
		const { data: user, response } = await client.GET('/api/users/me');

		if (response.ok && user) {
			return { user, error: null };
		}

		// 401/403 — not authenticated, not an infrastructure error
		if (response.status === 401 || response.status === 403) {
			return { user: null, error: null };
		}

		// 5xx — backend is up but returning server errors
		if (response.status >= 500) {
			console.error(`Backend error fetching user: ${response.status}`);
			return { user: null, error: 'backend_unavailable' };
		}

		return { user: null, error: null };
	} catch (e) {
		// Network error (ECONNREFUSED, timeout, etc.)
		console.error('Failed to fetch user:', e);
		return { user: null, error: 'backend_unavailable' };
	}
}

/**
 * Logs out the current user by revoking tokens and redirecting to login.
 */
export async function logout() {
	try {
		await browserClient.POST('/api/auth/logout');
	} catch (e) {
		// Logout may fail if tokens are already expired — that's fine,
		// the user is effectively logged out already. Proceed to redirect.
		console.warn('Logout request failed:', e);
	}
	await invalidateAll();
	await goto(resolve('/login'));
}
