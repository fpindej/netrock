import type { RequestHandler } from './$types';

export const GET: RequestHandler = ({ url }) => {
	const origin = url.origin;

	const body = [
		'User-agent: *',
		'Allow: /',
		'Disallow: /api/',
		'Disallow: /admin/',
		'Disallow: /dashboard',
		'Disallow: /profile',
		'Disallow: /settings',
		'',
		`Sitemap: ${origin}/sitemap.xml`
	].join('\n');

	return new Response(body, {
		headers: {
			'Content-Type': 'text/plain; charset=utf-8',
			'Cache-Control': 'public, max-age=86400'
		}
	});
};
