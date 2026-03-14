import type { RequestHandler } from './$types';

/**
 * Public routes to include in the sitemap.
 * Add your public marketing/landing pages here as you build them.
 * Auth pages (login, register, etc.) are excluded since they are
 * utility pages, not content pages you want search engines to rank.
 */
const publicRoutes = [{ path: '/', changefreq: 'weekly', priority: '1.0' }];

export const GET: RequestHandler = ({ url }) => {
	const origin = url.origin;

	const urls = publicRoutes
		.map(
			(route) => `  <url>
    <loc>${origin}${route.path}</loc>
    <changefreq>${route.changefreq}</changefreq>
    <priority>${route.priority}</priority>
  </url>`
		)
		.join('\n');

	const body = `<?xml version="1.0" encoding="UTF-8"?>
<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
${urls}
</urlset>`;

	return new Response(body, {
		headers: {
			'Content-Type': 'application/xml; charset=utf-8',
			'Cache-Control': 'public, max-age=3600'
		}
	});
};
