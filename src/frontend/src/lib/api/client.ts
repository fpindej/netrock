import createClient from 'openapi-fetch';
import type { paths } from './v1';

export const createApiClient = (customFetch?: typeof fetch, baseUrl: string = '') =>
	createClient<paths>({
		baseUrl,
		fetch: customFetch
	});

export const client = createApiClient();
