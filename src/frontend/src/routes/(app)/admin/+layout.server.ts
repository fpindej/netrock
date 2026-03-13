import { redirect } from '@sveltejs/kit';
import { adminRoutes, routes } from '$lib/config';
import { hasAnyPermission } from '$lib/utils';
import type { LayoutServerLoad } from './$types';

export const load: LayoutServerLoad = async ({ parent }) => {
	const { user } = await parent();

	// Parent (app) layout guarantees user is non-null (redirects to /login otherwise).
	const hasAdminAccess = hasAnyPermission(
		user,
		Object.values(adminRoutes).map((r) => r.permission)
	);
	if (!hasAdminAccess) {
		throw redirect(303, routes.dashboard);
	}

	return { user };
};
