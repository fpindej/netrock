import { error, redirect } from '@sveltejs/kit';
import type { LayoutServerLoad } from './$types';

/**
 * Must match the backend CookieNames.RefreshToken constant.
 *
 * The `__Secure-` prefix requires HTTPS per the cookie spec. Modern browsers
 * (Chrome, Firefox) accept it on `localhost` as a development exception, but
 * this is not spec-guaranteed. If the cookie is absent in local dev, the guard
 * degrades gracefully — users get a clean `/login` instead of "session expired."
 */
export const REFRESH_TOKEN_COOKIE = '__Secure-REFRESH-TOKEN';

export const load: LayoutServerLoad = async ({ parent, cookies }) => {
	const { user, backendError } = await parent();

	if (backendError === 'backend_unavailable') {
		throw error(503, 'Backend unavailable');
	}

	if (!user) {
		// Only show "session expired" when the user had an active session
		// (refresh token cookie present) that the backend rejected.
		// Fresh visitors with no cookies get a clean login page.
		//
		// Tradeoff: if the cookie itself expires naturally (Max-Age elapsed),
		// the browser stops sending it and the user sees a clean login instead
		// of "session expired." This is intentional — showing a stale expiry
		// message for a long-gone session would be more confusing than helpful.
		const hadSession = Boolean(cookies.get(REFRESH_TOKEN_COOKIE));
		const target = hadSession ? '/login?reason=session_expired' : '/login';
		throw redirect(303, target);
	}

	return {
		user
	};
};
