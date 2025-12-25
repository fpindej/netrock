import { env } from '$env/dynamic/private';
import type { RequestHandler } from './$types';

const API_URL = env.API_URL || 'http://localhost:13002';

export const GET: RequestHandler = async ({ fetch }) => {
	try {
		const response = await fetch(`${API_URL}/health`);
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
