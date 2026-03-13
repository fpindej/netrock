import { createApiClient, getErrorMessage } from '$lib/api';
import { error, redirect } from '@sveltejs/kit';
import { routes } from '$lib/config';
import { hasPermission, Permissions } from '$lib/utils';
import * as m from '$lib/paraglide/messages';
import type { PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ fetch, url, parent }) => {
	const { user } = await parent();
	if (!hasPermission(user, Permissions.OAuthProviders.View)) {
		throw redirect(303, routes.dashboard);
	}

	const client = createApiClient(fetch, url.origin);

	const { data, response, error: apiError } = await client.GET('/api/v1/admin/oauth-providers');

	if (!response.ok) {
		throw error(
			response.status,
			getErrorMessage(apiError, m.serverError_failedToLoadOAuthProviders())
		);
	}

	return {
		providers: data ?? []
	};
};
