import type { LayoutServerLoad } from './$types';
import { SERVER_CONFIG } from '$lib/config/server';

export const load: LayoutServerLoad = async () => {
	return {
		turnstileSiteKey: SERVER_CONFIG.TURNSTILE_SITE_KEY
	};
};
