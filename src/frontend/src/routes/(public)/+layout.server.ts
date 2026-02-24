import { error } from '@sveltejs/kit';
import * as m from '$lib/paraglide/messages';
import type { LayoutServerLoad } from './$types';
import { SERVER_CONFIG } from '$lib/config/server';

export const load: LayoutServerLoad = async ({ parent }) => {
	const { backendError } = await parent();

	if (backendError === 'backend_unavailable') {
		throw error(503, m.serverError_backendUnavailable());
	}

	return {
		turnstileSiteKey: SERVER_CONFIG.TURNSTILE_SITE_KEY
	};
};
