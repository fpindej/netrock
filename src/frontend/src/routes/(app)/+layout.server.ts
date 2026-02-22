import { error, redirect } from '@sveltejs/kit';
import type { LayoutServerLoad } from './$types';

/** Must match the backend CookieNames.RefreshToken constant. */
const REFRESH_TOKEN_COOKIE = '__Secure-REFRESH-TOKEN';

export const load: LayoutServerLoad = async ({ parent, cookies }) => {
	const { user, backendError } = await parent();

	if (backendError === 'backend_unavailable') {
		throw error(503, 'Backend unavailable');
	}

	if (!user) {
		// Only show "session expired" when the user had an active session
		// (refresh token cookie present) that the backend rejected.
		// Fresh visitors with no cookies get a clean login page.
		const hadSession = Boolean(cookies.get(REFRESH_TOKEN_COOKIE));
		const target = hadSession ? '/login?reason=session_expired' : '/login';
		throw redirect(303, target);
	}

	return {
		user
	};
};
