import { redirect } from '@sveltejs/kit';
import { hasAnyPermission, Permissions } from '$lib/utils';
import type { LayoutServerLoad } from './$types';

export const load: LayoutServerLoad = async ({ parent }) => {
	const { user } = await parent();

	// Parent (app) layout guarantees user is non-null (redirects to /login otherwise).
	const hasAdminAccess = hasAnyPermission(user, [
		Permissions.Users.View,
		Permissions.Roles.View,
		Permissions.Jobs.View
	]);
	if (!hasAdminAccess) {
		throw redirect(303, '/');
	}

	return { user };
};
