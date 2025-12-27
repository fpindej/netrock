import type { Handle } from '@sveltejs/kit';
import { paraglideMiddleware } from '$lib/paraglide/server';

export const handle: Handle = async ({ event, resolve }) => {
	// Skip auth check for API routes to avoid infinite loops
	if (event.url.pathname.startsWith('/api')) {
		return resolve(event);
	}

	return paraglideMiddleware(event.request, async ({ locale }) => {
		event.locals.locale = locale;
		event.locals.user = null;

		return resolve(event, {
			transformPageChunk: ({ html }) => html.replace('%lang%', locale)
		});
	});
};
