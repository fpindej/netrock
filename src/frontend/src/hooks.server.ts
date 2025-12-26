import type { Handle } from '@sveltejs/kit';
import { defaultLocale, supportedLocales } from '$lib/i18n';

export const handle: Handle = async ({ event, resolve }) => {
	// Skip auth check for API routes to avoid infinite loops
	if (event.url.pathname.startsWith('/api')) {
		return resolve(event);
	}

	const cookieLang = event.cookies.get('locale');
	const headerLang = event.request.headers.get('accept-language')?.split(',')[0];
	const lang = cookieLang || headerLang;

	const foundLocale = supportedLocales.find((l) => lang?.startsWith(l));
	const locale = foundLocale || defaultLocale;
	event.locals.locale = locale;
	event.locals.user = null;

	return resolve(event, {
		transformPageChunk: ({ html }) => html.replace('%lang%', event.locals.locale)
	});
};
