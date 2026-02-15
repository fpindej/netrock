import type { PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ url }) => {
	return {
		email: url.searchParams.get('email') ?? '',
		token: url.searchParams.get('token') ?? ''
	};
};
