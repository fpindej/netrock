import type { PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ parent, url }) => {
	const { user } = await parent();

	return {
		token: url.searchParams.get('token') ?? '',
		invited: url.searchParams.has('invited'),
		user: user ?? null
	};
};
