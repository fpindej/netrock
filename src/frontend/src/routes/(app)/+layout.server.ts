import { error, redirect } from '@sveltejs/kit';
import { routes } from '$lib/config';
import * as m from '$lib/paraglide/messages';
import type { LayoutServerLoad } from './$types';

export const load: LayoutServerLoad = async ({ parent, cookies }) => {
	const { user, backendError, hadSession } = await parent();

	if (backendError === 'backend_unavailable') {
		throw error(503, m.serverError_backendUnavailable());
	}

	if (!user) {
		// Only show "session expired" when the user had an active session
		// (refresh token cookie was present in the original request).
		// Fresh visitors with no cookies get a clean login page.
		//
		// hadSession is read in the root layout BEFORE getUser() runs, because
		// a failed token refresh causes the backend to send Set-Cookie with
		// Max-Age=0, and SvelteKit's internal fetch mutates the shared cookie
		// store - reading the cookie after getUser would always return undefined.
		//
		// Tradeoff: if the cookie itself expires naturally (Max-Age elapsed),
		// the browser stops sending it and the user sees a clean login instead
		// of "session expired." This is intentional - showing a stale expiry
		// message for a long-gone session would be more confusing than helpful.
		const target = hadSession ? `${routes.login}?reason=session_expired` : routes.login;
		throw redirect(303, target);
	}

	// Read sidebar cookie for SSR-correct initial state (avoids flash on page load).
	// The shadcn SidebarProvider persists state to this cookie on toggle.
	const sidebarOpen = cookies.get('sidebar:state') !== 'false';

	return { user, sidebarOpen };
};
