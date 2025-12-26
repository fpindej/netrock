import { createApiClient } from '$lib/api/client';
import type { components } from '$lib/api/v1';

type User = components['schemas']['MeResponse'];

export async function getUser(
	fetch: typeof globalThis.fetch,
	origin: string
): Promise<User | null> {
	const client = createApiClient(fetch, origin);

	try {
		const { data: user, response } = await client.GET('/api/auth/me');
		if (response.ok && user) {
			return user;
		}

		if (response.status === 401) {
			// Try to refresh the token
			const { response: refreshResponse } = await client.POST('/api/auth/refresh');
			if (refreshResponse.ok) {
				// Retry fetching the user
				const { data: retryUser, response: retryResponse } = await client.GET('/api/auth/me');
				if (retryResponse.ok && retryUser) {
					return retryUser;
				}
			}
		}
	} catch (e) {
		console.error('Failed to fetch user:', e);
	}

	return null;
}
