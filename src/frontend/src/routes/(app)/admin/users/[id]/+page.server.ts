import { createApiClient, getErrorMessage } from '$lib/api';
import { error, redirect } from '@sveltejs/kit';
import { hasPermission, Permissions } from '$lib/utils';
import * as m from '$lib/paraglide/messages';
import type { PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ fetch, url, params, parent }) => {
	const { user } = await parent();
	if (!hasPermission(user, Permissions.Users.View)) {
		throw redirect(303, '/');
	}

	const client = createApiClient(fetch, url.origin);

	const [userResult, rolesResult] = await Promise.all([
		client.GET('/api/v1/admin/users/{id}', {
			params: { path: { id: params.id } }
		}),
		client.GET('/api/v1/admin/roles')
	]);

	if (!userResult.response.ok) {
		throw error(
			userResult.response.status,
			getErrorMessage(userResult.error, m.serverError_failedToLoadUserDetails())
		);
	}

	const rolesLoadFailed = !rolesResult.response.ok;
	if (rolesLoadFailed) {
		console.warn(
			`Failed to load roles list (HTTP ${rolesResult.response.status}) — ` +
				'user detail page will render without role assignment options'
		);
	}

	return {
		adminUser: userResult.data,
		roles: rolesLoadFailed ? [] : (rolesResult.data ?? []),
		rolesLoadFailed
	};
};
