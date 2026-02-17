import { redirect } from '@sveltejs/kit';
import type { PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ parent, url }) => {
	const { user } = await parent();

	if (user) {
		throw redirect(303, '/');
	}

	return {
		token: url.searchParams.get('token') ?? ''
	};
};
