import { error, redirect } from '@sveltejs/kit';
import type { LayoutServerLoad } from './$types';

export const load: LayoutServerLoad = async ({ parent }) => {
	const { user, backendError } = await parent();

	if (backendError === 'backend_unavailable') {
		throw error(503, 'Backend unavailable');
	}

	if (!user) {
		throw redirect(303, '/login?reason=session_expired');
	}

	return {
		user
	};
};
