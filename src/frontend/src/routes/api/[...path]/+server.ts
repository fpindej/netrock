import type { RequestHandler } from './$types';
import { SERVER_CONFIG } from '$lib/config/server';
import { isFetchErrorWithCode } from '$lib/api';

/** Auth endpoints that need cookie-based auth for web clients */
const COOKIE_AUTH_ENDPOINTS = ['auth/login', 'auth/refresh'];

/** HTTP methods that can mutate state and are vulnerable to CSRF */
const UNSAFE_METHODS = ['POST', 'PUT', 'PATCH', 'DELETE'];

/**
 * Request headers forwarded to the backend.
 * Only headers the API needs are proxied — everything else is stripped
 * to avoid leaking browser metadata to internal services.
 */
const FORWARDED_REQUEST_HEADERS = [
	'accept',
	'accept-language',
	'authorization',
	'content-type',
	'content-length',
	'cookie',
	'if-match',
	'if-none-match',
	'if-modified-since'
];

/**
 * Response headers stripped before sending to the browser.
 * Prevents leaking internal infrastructure details and removes
 * hop-by-hop headers that are meaningless across the proxy boundary.
 */
const STRIPPED_RESPONSE_HEADERS = [
	'connection',
	'keep-alive',
	'transfer-encoding',
	'server',
	'x-powered-by'
];

/**
 * Validates that the request Origin header matches the app's own origin
 * or an explicitly configured allowed origin (ALLOWED_ORIGINS env var).
 * This prevents cross-site request forgery for cookie-authenticated requests
 * proxied through SvelteKit (SameSite=None cookies are sent cross-origin).
 *
 * Only enforced for state-changing methods — GET/HEAD/OPTIONS pass through.
 * Does not affect mobile or API-key clients since they call the backend directly.
 */
function isOriginAllowed(request: Request, url: URL): boolean {
	if (!UNSAFE_METHODS.includes(request.method)) {
		return true;
	}

	const origin = request.headers.get('origin');

	// Browsers always send Origin on cross-origin requests and on same-origin
	// POST/PUT/PATCH/DELETE. A missing Origin on an unsafe method means either
	// a same-origin request from an older browser or a non-browser client —
	// both are safe to allow through.
	if (!origin) {
		return true;
	}

	if (origin === url.origin) {
		return true;
	}

	return SERVER_CONFIG.ALLOWED_ORIGINS.includes(origin);
}

/** Build a filtered copy of request headers using the allowlist. */
function filterRequestHeaders(source: Headers): Headers {
	const filtered = new Headers();
	for (const name of FORWARDED_REQUEST_HEADERS) {
		const value = source.get(name);
		if (value) {
			filtered.set(name, value);
		}
	}
	return filtered;
}

/** Remove unwanted headers from the backend response. */
function stripResponseHeaders(response: Response): Response {
	const headers = new Headers(response.headers);
	for (const name of STRIPPED_RESPONSE_HEADERS) {
		headers.delete(name);
	}
	return new Response(response.body, {
		status: response.status,
		statusText: response.statusText,
		headers
	});
}

export const fallback: RequestHandler = async ({ request, params, url, fetch }) => {
	if (!isOriginAllowed(request, url)) {
		return new Response(JSON.stringify({ message: 'Cross-origin requests are not allowed' }), {
			status: 403,
			headers: { 'Content-Type': 'application/json' }
		});
	}

	// Build target URL with query string
	const targetParams = new URLSearchParams(url.search);

	// Web clients need cookies for auth endpoints
	if (COOKIE_AUTH_ENDPOINTS.includes(params.path)) {
		targetParams.set('useCookies', 'true');
	}

	const queryString = targetParams.toString();
	const targetUrl = `${SERVER_CONFIG.API_URL}/api/${params.path}${queryString ? `?${queryString}` : ''}`;

	const newRequest = new Request(targetUrl, {
		method: request.method,
		headers: filterRequestHeaders(request.headers),
		body: request.body,
		// @ts-expect-error - duplex is needed for streaming bodies in some node versions/fetch implementations
		duplex: 'half'
	});

	try {
		const response = await fetch(newRequest);
		return stripResponseHeaders(response);
	} catch (err) {
		console.error('Proxy error:', err);

		if (isFetchErrorWithCode(err, 'ECONNREFUSED')) {
			return new Response(JSON.stringify({ message: 'Backend unavailable' }), {
				status: 503,
				headers: { 'Content-Type': 'application/json' }
			});
		}

		return new Response('Bad Gateway', { status: 502 });
	}
};
