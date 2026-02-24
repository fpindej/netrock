import type { PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ parent, url }) => {
	const { user } = await parent();

	// Unlike /login and /forgot-password, this page intentionally does NOT
	// redirect authenticated users away. An already-signed-in user may arrive
	// here via an admin-sent invitation link and needs to see the page so they
	// can sign out and continue with the invite flow.
	return {
		token: url.searchParams.get('token') ?? '',
		invited: url.searchParams.has('invited'),
		user: user ?? null
	};
};
