import type { LayoutServerLoad } from './$types';
import { dev } from '$app/environment';
import { SERVER_CONFIG } from '$lib/config/server';
import { getUser, REFRESH_TOKEN_COOKIE } from '$lib/auth';

export const load: LayoutServerLoad = async ({ locals, fetch, url, cookies }) => {
	// Read BEFORE getUser — the failed-refresh response includes a Set-Cookie
	// that clears the token, and SvelteKit's internal fetch mutates the shared
	// cookie store. After getUser returns, cookies.get() would return undefined.
	const hadSession = Boolean(cookies.get(REFRESH_TOKEN_COOKIE));

	const { user, error: backendError } = await getUser(fetch, url.origin);
	return {
		user,
		backendError,
		hadSession,
		locale: locals.locale,
		apiUrl: dev ? SERVER_CONFIG.API_URL : undefined
	};
};
