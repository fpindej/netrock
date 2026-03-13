import { redirect } from '@sveltejs/kit';
import { routes } from '$lib/config';
import type { PageServerLoad } from './$types';

export const load: PageServerLoad = async () => {
	throw redirect(301, routes.dashboard);
};
