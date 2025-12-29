import type { RequestHandler } from './$types';
import { SERVER_CONFIG } from '$lib/config/server';

export const GET: RequestHandler = async ({ fetch }) => {
	try {
		const response = await fetch(`${SERVER_CONFIG.API_URL}/health`);
		return new Response(response.body, {
			status: response.status,
			headers: {
				'Content-Type': 'text/plain'
			}
		});
	} catch {
		return new Response('Offline', { status: 503 });
	}
};
