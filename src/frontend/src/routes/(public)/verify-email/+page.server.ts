import type { PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ url }) => {
	return {
		token: url.searchParams.get('token') ?? ''
	};
};
