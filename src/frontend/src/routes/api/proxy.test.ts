/**
 * Tests for the BFF API proxy — the catch-all route that forwards requests
 * from the SvelteKit frontend to the .NET backend.
 *
 * The proxy has three main responsibilities:
 * 1. CSRF protection — reject cross-origin state-changing requests
 * 2. Header filtering — allowlist request headers, strip response headers
 * 3. Error handling — return ProblemDetails for connection failures
 *
 * All helper functions (isOriginAllowed, filterRequestHeaders, stripResponseHeaders)
 * are module-private, so they are tested indirectly through the fallback handler.
 */
import { describe, expect, it, vi, beforeEach } from 'vitest';

vi.mock('$lib/config/server', () => ({
	SERVER_CONFIG: {
		API_URL: 'http://backend:8080',
		ALLOWED_ORIGINS: ['https://allowed.example.com', 'https://ngrok.example.com']
	}
}));

vi.mock('$lib/api', () => ({
	isFetchErrorWithCode: (error: unknown, code: string): boolean => {
		if (typeof error !== 'object' || error === null) return false;
		const cause = (error as { cause?: { code?: string } }).cause;
		return cause?.code === code;
	}
}));

// Import AFTER mocks are set up
const { fallback } = await import('./[...path]/+server');

type FallbackEvent = Parameters<typeof fallback>;

/** Builds a mock RequestEvent for the proxy handler. */
function mockProxyEvent(
	overrides: {
		method?: string;
		path?: string;
		origin?: string;
		headers?: Record<string, string>;
		search?: string;
		body?: BodyInit | null;
		fetchResponse?: Response;
		fetchError?: Error;
		clientAddress?: string;
	} = {}
) {
	const {
		method = 'GET',
		path = 'users',
		origin,
		headers = {},
		search = '',
		body = null,
		fetchResponse = new Response(JSON.stringify({ ok: true }), {
			status: 200,
			headers: { 'Content-Type': 'application/json' }
		}),
		fetchError,
		clientAddress = '192.168.1.100'
	} = overrides;

	const url = new URL(`http://localhost:5173/api/${path}${search}`);
	const requestHeaders = new Headers(headers);
	if (origin) {
		requestHeaders.set('origin', origin);
	}

	const requestInit: RequestInit = { method, headers: requestHeaders };
	if (body && method !== 'GET' && method !== 'HEAD') {
		requestInit.body = body;
	}
	const request = new Request(url.toString(), requestInit);

	const mockFetch = fetchError
		? vi.fn().mockRejectedValue(fetchError)
		: vi.fn().mockResolvedValue(fetchResponse);

	return {
		request,
		params: { path },
		url,
		fetch: mockFetch as typeof fetch,
		getClientAddress: () => clientAddress,
		cookies: {
			get: vi.fn(),
			getAll: vi.fn(() => []),
			set: vi.fn(),
			delete: vi.fn(),
			serialize: vi.fn()
		},
		locals: { user: null, locale: 'en' },
		platform: undefined,
		route: { id: '/api/[...path]' },
		setHeaders: vi.fn(),
		isDataRequest: false,
		isSubRequest: false,
		isRemoteRequest: false,
		tracing: { enabled: false, root: {}, current: {} }
	} as unknown as FallbackEvent[0];
}

/** Parses the response body as a ProblemDetails object. */
async function parseProblemDetails(response: Response) {
	return JSON.parse(await response.text()) as {
		type: string;
		title: string;
		status: number;
		detail: string;
	};
}

/** Extracts the Request object passed to the mocked fetch call. */
function getProxiedRequest(event: FallbackEvent[0]): Request {
	const call = vi.mocked(event.fetch).mock.calls[0];
	if (!call) throw new Error('Expected fetch to have been called');
	return call[0] as Request;
}

describe('API proxy — CSRF origin validation', () => {
	// ── Safe methods pass through without origin check ──────────────

	it('GET request without origin — allowed', async () => {
		const event = mockProxyEvent({ method: 'GET' });
		const response = await fallback(event);
		expect(response.status).toBe(200);
	});

	it('GET request with cross-origin — allowed (safe method)', async () => {
		const event = mockProxyEvent({
			method: 'GET',
			origin: 'https://evil.example.com'
		});
		const response = await fallback(event);
		expect(response.status).toBe(200);
	});

	// ── Unsafe methods with matching origins ────────────────────────

	it('POST with same origin — allowed', async () => {
		const event = mockProxyEvent({
			method: 'POST',
			origin: 'http://localhost:5173',
			headers: { 'content-type': 'application/json' },
			body: '{}'
		});
		const response = await fallback(event);
		expect(response.status).toBe(200);
	});

	it('PUT with same origin — allowed', async () => {
		const event = mockProxyEvent({
			method: 'PUT',
			origin: 'http://localhost:5173',
			headers: { 'content-type': 'application/json' },
			body: '{}'
		});
		const response = await fallback(event);
		expect(response.status).toBe(200);
	});

	it('DELETE with same origin — allowed', async () => {
		const event = mockProxyEvent({
			method: 'DELETE',
			origin: 'http://localhost:5173'
		});
		const response = await fallback(event);
		expect(response.status).toBe(200);
	});

	it('PATCH with same origin — allowed', async () => {
		const event = mockProxyEvent({
			method: 'PATCH',
			origin: 'http://localhost:5173',
			headers: { 'content-type': 'application/json' },
			body: '{}'
		});
		const response = await fallback(event);
		expect(response.status).toBe(200);
	});

	// ── Unsafe methods without origin (non-browser or same-origin older browser) ─

	it('POST without origin header — allowed (non-browser or legacy same-origin)', async () => {
		const event = mockProxyEvent({
			method: 'POST',
			headers: { 'content-type': 'application/json' },
			body: '{}'
		});
		const response = await fallback(event);
		expect(response.status).toBe(200);
	});

	// ── Unsafe methods with explicitly allowed origin ───────────────

	it('POST with explicitly allowed origin — allowed', async () => {
		const event = mockProxyEvent({
			method: 'POST',
			origin: 'https://allowed.example.com',
			headers: { 'content-type': 'application/json' },
			body: '{}'
		});
		const response = await fallback(event);
		expect(response.status).toBe(200);
	});

	it('DELETE with second allowed origin — allowed', async () => {
		const event = mockProxyEvent({
			method: 'DELETE',
			origin: 'https://ngrok.example.com'
		});
		const response = await fallback(event);
		expect(response.status).toBe(200);
	});

	// ── Unsafe methods with disallowed origin — CSRF rejection ──────

	it('POST with cross-origin — returns 403 ProblemDetails', async () => {
		const event = mockProxyEvent({
			method: 'POST',
			origin: 'https://evil.example.com',
			headers: { 'content-type': 'application/json' },
			body: '{}'
		});
		const response = await fallback(event);

		expect(response.status).toBe(403);
		expect(response.headers.get('Content-Type')).toBe('application/problem+json');

		const body = await parseProblemDetails(response);
		expect(body.title).toBe('Forbidden');
		expect(body.detail).toBe('Cross-origin requests are not allowed');
	});

	it('PUT with cross-origin — returns 403', async () => {
		const event = mockProxyEvent({
			method: 'PUT',
			origin: 'https://attacker.com',
			headers: { 'content-type': 'application/json' },
			body: '{}'
		});
		const response = await fallback(event);
		expect(response.status).toBe(403);
	});

	it('DELETE with cross-origin — returns 403', async () => {
		const event = mockProxyEvent({
			method: 'DELETE',
			origin: 'https://attacker.com'
		});
		const response = await fallback(event);
		expect(response.status).toBe(403);
	});

	it('PATCH with cross-origin — returns 403', async () => {
		const event = mockProxyEvent({
			method: 'PATCH',
			origin: 'https://attacker.com',
			headers: { 'content-type': 'application/json' },
			body: '{}'
		});
		const response = await fallback(event);
		expect(response.status).toBe(403);
	});

	it('origin comparison is exact — port mismatch is rejected', async () => {
		const event = mockProxyEvent({
			method: 'POST',
			origin: 'http://localhost:9999',
			headers: { 'content-type': 'application/json' },
			body: '{}'
		});
		const response = await fallback(event);
		expect(response.status).toBe(403);
	});

	it('origin comparison is case-sensitive — uppercase scheme rejected', async () => {
		const event = mockProxyEvent({
			method: 'POST',
			origin: 'HTTP://LOCALHOST:5173',
			headers: { 'content-type': 'application/json' },
			body: '{}'
		});
		const response = await fallback(event);
		// Browsers normalize origin to lowercase, but if somehow an uppercase
		// origin is sent, it should not match and should be rejected.
		expect(response.status).toBe(403);
	});

	it('CSRF rejection does not call backend fetch', async () => {
		const event = mockProxyEvent({
			method: 'POST',
			origin: 'https://evil.example.com',
			headers: { 'content-type': 'application/json' },
			body: '{}'
		});
		await fallback(event);
		expect(event.fetch).not.toHaveBeenCalled();
	});
});

describe('API proxy — request header filtering', () => {
	it('forwards allowlisted headers to backend', async () => {
		const event = mockProxyEvent({
			headers: {
				accept: 'application/json',
				'accept-language': 'en-US',
				authorization: 'Bearer token123',
				'content-type': 'application/json',
				cookie: 'session=abc',
				'if-match': '"etag-value"',
				'if-none-match': '"etag-other"',
				'if-modified-since': 'Wed, 21 Oct 2025 07:28:00 GMT'
			}
		});

		await fallback(event);

		const proxiedHeaders = getProxiedRequest(event).headers;

		expect(proxiedHeaders.get('accept')).toBe('application/json');
		expect(proxiedHeaders.get('accept-language')).toBe('en-US');
		expect(proxiedHeaders.get('authorization')).toBe('Bearer token123');
		expect(proxiedHeaders.get('content-type')).toBe('application/json');
		expect(proxiedHeaders.get('cookie')).toBe('session=abc');
		expect(proxiedHeaders.get('if-match')).toBe('"etag-value"');
		expect(proxiedHeaders.get('if-none-match')).toBe('"etag-other"');
		expect(proxiedHeaders.get('if-modified-since')).toBe('Wed, 21 Oct 2025 07:28:00 GMT');
	});

	it('strips non-allowlisted headers (host, connection, user-agent)', async () => {
		const event = mockProxyEvent({
			headers: {
				host: 'localhost:5173',
				connection: 'keep-alive',
				'user-agent': 'Mozilla/5.0',
				referer: 'http://localhost:5173/dashboard',
				'sec-fetch-mode': 'cors',
				'x-custom-header': 'should-be-stripped'
			}
		});

		await fallback(event);

		const proxiedHeaders = getProxiedRequest(event).headers;

		expect(proxiedHeaders.has('host')).toBe(false);
		expect(proxiedHeaders.has('connection')).toBe(false);
		expect(proxiedHeaders.has('user-agent')).toBe(false);
		expect(proxiedHeaders.has('referer')).toBe(false);
		expect(proxiedHeaders.has('sec-fetch-mode')).toBe(false);
		expect(proxiedHeaders.has('x-custom-header')).toBe(false);
	});

	it('sets x-forwarded-for from getClientAddress()', async () => {
		const event = mockProxyEvent({ clientAddress: '10.0.0.42' });

		await fallback(event);

		expect(getProxiedRequest(event).headers.get('x-forwarded-for')).toBe('10.0.0.42');
	});

	it('forwards x-forwarded-proto when present', async () => {
		const event = mockProxyEvent({
			headers: { 'x-forwarded-proto': 'https' }
		});

		await fallback(event);

		expect(getProxiedRequest(event).headers.get('x-forwarded-proto')).toBe('https');
	});

	it('omits x-forwarded-proto when not in original request', async () => {
		const event = mockProxyEvent();

		await fallback(event);

		expect(getProxiedRequest(event).headers.has('x-forwarded-proto')).toBe(false);
	});
});

describe('API proxy — response header stripping', () => {
	it('preserves content-type from backend response', async () => {
		const event = mockProxyEvent({
			fetchResponse: new Response('{}', {
				headers: { 'Content-Type': 'application/json' }
			})
		});

		const response = await fallback(event);
		expect(response.headers.get('content-type')).toBe('application/json');
	});

	it('strips transfer-encoding from backend response', async () => {
		const event = mockProxyEvent({
			fetchResponse: new Response('{}', {
				headers: {
					'Content-Type': 'application/json',
					'Transfer-Encoding': 'chunked'
				}
			})
		});

		const response = await fallback(event);
		expect(response.headers.has('transfer-encoding')).toBe(false);
	});

	it('strips server header from backend response', async () => {
		const event = mockProxyEvent({
			fetchResponse: new Response('{}', {
				headers: {
					'Content-Type': 'application/json',
					Server: 'Kestrel'
				}
			})
		});

		const response = await fallback(event);
		expect(response.headers.has('server')).toBe(false);
	});

	it('strips x-powered-by from backend response', async () => {
		const event = mockProxyEvent({
			fetchResponse: new Response('{}', {
				headers: {
					'Content-Type': 'application/json',
					'X-Powered-By': 'ASP.NET'
				}
			})
		});

		const response = await fallback(event);
		expect(response.headers.has('x-powered-by')).toBe(false);
	});

	it('strips connection and keep-alive from backend response', async () => {
		const event = mockProxyEvent({
			fetchResponse: new Response('{}', {
				headers: {
					'Content-Type': 'application/json',
					Connection: 'keep-alive',
					'Keep-Alive': 'timeout=5'
				}
			})
		});

		const response = await fallback(event);
		expect(response.headers.has('connection')).toBe(false);
		expect(response.headers.has('keep-alive')).toBe(false);
	});

	it('preserves backend response status and statusText', async () => {
		const event = mockProxyEvent({
			fetchResponse: new Response(null, { status: 204, statusText: 'No Content' })
		});

		const response = await fallback(event);
		expect(response.status).toBe(204);
		expect(response.statusText).toBe('No Content');
	});
});

describe('API proxy — URL construction and cookie auth paths', () => {
	it('proxies to the correct backend URL', async () => {
		const event = mockProxyEvent({ path: 'users/123' });

		await fallback(event);

		expect(getProxiedRequest(event).url).toBe('http://backend:8080/api/users/123');
	});

	it('forwards query parameters from original request', async () => {
		const event = mockProxyEvent({ path: 'users', search: '?page=2&size=10' });

		await fallback(event);

		const targetUrl = new URL(getProxiedRequest(event).url);

		expect(targetUrl.searchParams.get('page')).toBe('2');
		expect(targetUrl.searchParams.get('size')).toBe('10');
	});

	it('appends useCookies=true for auth/login path', async () => {
		const event = mockProxyEvent({ path: 'auth/login' });

		await fallback(event);

		const targetUrl = new URL(getProxiedRequest(event).url);

		expect(targetUrl.searchParams.get('useCookies')).toBe('true');
	});

	it('appends useCookies=true for auth/refresh path', async () => {
		const event = mockProxyEvent({ path: 'auth/refresh' });

		await fallback(event);

		const targetUrl = new URL(getProxiedRequest(event).url);

		expect(targetUrl.searchParams.get('useCookies')).toBe('true');
	});

	it('appends useCookies=true for auth/login/2fa path', async () => {
		const event = mockProxyEvent({ path: 'auth/login/2fa' });

		await fallback(event);

		const targetUrl = new URL(getProxiedRequest(event).url);

		expect(targetUrl.searchParams.get('useCookies')).toBe('true');
	});

	it('appends useCookies=true for auth/login/2fa/recovery path', async () => {
		const event = mockProxyEvent({ path: 'auth/login/2fa/recovery' });

		await fallback(event);

		const targetUrl = new URL(getProxiedRequest(event).url);

		expect(targetUrl.searchParams.get('useCookies')).toBe('true');
	});

	it('does not append useCookies for non-auth paths', async () => {
		const event = mockProxyEvent({ path: 'users' });

		await fallback(event);

		const targetUrl = new URL(getProxiedRequest(event).url);

		expect(targetUrl.searchParams.has('useCookies')).toBe(false);
	});

	it('preserves HTTP method in proxied request', async () => {
		const event = mockProxyEvent({
			method: 'PUT',
			origin: 'http://localhost:5173',
			headers: { 'content-type': 'application/json' },
			body: '{"name":"updated"}'
		});

		await fallback(event);

		expect(getProxiedRequest(event).method).toBe('PUT');
	});
});

describe('API proxy — error handling', () => {
	beforeEach(() => {
		vi.spyOn(console, 'error').mockImplementation(() => {});
	});

	it('ECONNREFUSED — returns 503 ProblemDetails', async () => {
		const connRefusedError = new TypeError('fetch failed');
		Object.assign(connRefusedError, { cause: { code: 'ECONNREFUSED' } });

		const event = mockProxyEvent({ fetchError: connRefusedError });
		const response = await fallback(event);

		expect(response.status).toBe(503);
		expect(response.headers.get('Content-Type')).toBe('application/problem+json');

		const body = await parseProblemDetails(response);
		expect(body.title).toBe('Service Unavailable');
		expect(body.detail).toBe('Backend unavailable');
	});

	it('unexpected fetch error — returns 502 ProblemDetails', async () => {
		const event = mockProxyEvent({
			fetchError: new Error('DNS resolution failed')
		});
		const response = await fallback(event);

		expect(response.status).toBe(502);
		expect(response.headers.get('Content-Type')).toBe('application/problem+json');

		const body = await parseProblemDetails(response);
		expect(body.title).toBe('Bad Gateway');
		expect(body.detail).toBe('An unexpected error occurred while proxying the request');
	});

	it('fetch errors are logged to console', async () => {
		const error = new Error('network failure');
		const event = mockProxyEvent({ fetchError: error });

		await fallback(event);

		expect(console.error).toHaveBeenCalledWith('Proxy error:', error);
	});
});
