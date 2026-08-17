import { isRedirect, redirect } from '@sveltejs/kit';
import { getErrorCode } from '$lib/api';
import { routes } from '$lib/config';
import type { PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ url, fetch }) => {
	const code = url.searchParams.get('code');
	const state = url.searchParams.get('state');
	const error = url.searchParams.get('error');

	// User denied consent at provider
	if (error) {
		return { error: 'provider_denied' };
	}

	if (!code || !state) {
		return { error: 'missing_params' };
	}

	try {
		const response = await fetch('/api/auth/external/callback', {
			method: 'POST',
			headers: { 'Content-Type': 'application/json' },
			body: JSON.stringify({ code, state })
		});

		if (!response.ok) {
			const body: unknown = await response.json().catch(() => null);
			return { error: getErrorCode(body) ?? 'unknown_error' };
		}

		const data = await response.json();

		if (data.isLinkOnly) {
			// Account linking from settings - redirect back to settings
			throw redirect(303, routes.settings);
		}
	} catch (e) {
		if (isRedirect(e)) throw e;
		return { error: 'network_error' };
	}

	// Successful login - redirect to dashboard
	throw redirect(303, routes.dashboard);
};
