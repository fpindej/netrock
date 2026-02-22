import { error } from '@sveltejs/kit';
import type { LayoutServerLoad } from './$types';
import { SERVER_CONFIG } from '$lib/config/server';

export const load: LayoutServerLoad = async ({ parent }) => {
	const { backendError } = await parent();

	if (backendError === 'backend_unavailable') {
		throw error(503, 'Backend unavailable');
	}

	return {
		turnstileSiteKey: SERVER_CONFIG.TURNSTILE_SITE_KEY
	};
};
