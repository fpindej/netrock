/**
 * Tests for the SvelteKit server hooks — security headers and API proxy bypass.
 *
 * The handle function applies security headers to all page responses but
 * skips them for API proxy routes (/api/*) where the backend sets its own.
 * HSTS is only applied in production mode.
 *
 * The paraglide middleware is mocked to pass through directly so these tests
 * focus on the security header logic.
 */
import { describe, expect, it, vi, beforeEach } from 'vitest';
import type { Handle, RequestEvent, ResolveOptions } from '@sveltejs/kit';

/** The expected security headers on every page response. */
const EXPECTED_HEADERS: Record<string, string> = {
	'X-Content-Type-Options': 'nosniff',
	'X-Frame-Options': 'DENY',
	'Referrer-Policy': 'strict-origin-when-cross-origin',
	'Permissions-Policy': 'camera=(), microphone=(), geolocation=()'
};

const HSTS_VALUE = 'max-age=63072000; includeSubDomains; preload';

vi.mock('$lib/paraglide/server', () => ({
	paraglideMiddleware: vi.fn(
		(_request: Request, resolve: (args: { locale: string }) => Response | Promise<Response>) =>
			resolve({ locale: 'en' })
	)
}));

vi.mock('$lib/paraglide/runtime', () => ({
	extractLocaleFromHeader: vi.fn(() => null),
	cookieName: 'PARAGLIDE_LOCALE',
	baseLocale: 'en'
}));

/** Creates a minimal mock RequestEvent for the handle function. */
function mockRequestEvent(pathname: string): RequestEvent {
	const url = new URL(`http://localhost${pathname}`);
	return {
		url,
		request: new Request(url),
		cookies: {
			get: vi.fn(() => undefined),
			getAll: vi.fn(() => []),
			set: vi.fn(),
			delete: vi.fn(),
			serialize: vi.fn()
		},
		locals: { user: null, locale: 'en' },
		params: {},
		platform: undefined,
		route: { id: pathname },
		getClientAddress: () => '127.0.0.1',
		setHeaders: vi.fn(),
		isDataRequest: false,
		isSubRequest: false,
		isRemoteRequest: false,
		fetch: vi.fn() as typeof fetch
	} as unknown as RequestEvent;
}

/** Creates a mock resolve function that returns a basic HTML response. */
function mockResolve(): (event: RequestEvent, opts?: ResolveOptions) => Promise<Response> {
	return vi.fn(async (_event: RequestEvent, opts?: ResolveOptions) => {
		let html = '<html lang="%lang%"><body>test</body></html>';
		if (opts?.transformPageChunk) {
			html = opts.transformPageChunk({ html, done: true }) as string;
		}
		return new Response(html, {
			status: 200,
			headers: { 'Content-Type': 'text/html' }
		});
	});
}

describe('hooks.server handle — production mode (dev: false)', () => {
	let handle: Handle;

	beforeEach(async () => {
		vi.resetModules();

		vi.doMock('$app/environment', () => ({
			browser: false,
			dev: false,
			building: false,
			version: 'test'
		}));

		vi.doMock('$lib/paraglide/server', () => ({
			paraglideMiddleware: vi.fn(
				(_request: Request, resolve: (args: { locale: string }) => Response | Promise<Response>) =>
					resolve({ locale: 'en' })
			)
		}));

		vi.doMock('$lib/paraglide/runtime', () => ({
			extractLocaleFromHeader: vi.fn(() => null),
			cookieName: 'PARAGLIDE_LOCALE',
			baseLocale: 'en'
		}));

		const mod = await import('./hooks.server');
		handle = mod.handle;
	});

	// ── Security headers on page responses ──────────────────────────

	it('page response — includes X-Content-Type-Options', async () => {
		const response = await handle({
			event: mockRequestEvent('/dashboard'),
			resolve: mockResolve()
		});
		expect(response.headers.get('X-Content-Type-Options')).toBe(
			EXPECTED_HEADERS['X-Content-Type-Options']
		);
	});

	it('page response — includes X-Frame-Options', async () => {
		const response = await handle({
			event: mockRequestEvent('/dashboard'),
			resolve: mockResolve()
		});
		expect(response.headers.get('X-Frame-Options')).toBe(EXPECTED_HEADERS['X-Frame-Options']);
	});

	it('page response — includes Referrer-Policy', async () => {
		const response = await handle({
			event: mockRequestEvent('/dashboard'),
			resolve: mockResolve()
		});
		expect(response.headers.get('Referrer-Policy')).toBe(EXPECTED_HEADERS['Referrer-Policy']);
	});

	it('page response — includes Permissions-Policy', async () => {
		const response = await handle({
			event: mockRequestEvent('/dashboard'),
			resolve: mockResolve()
		});
		expect(response.headers.get('Permissions-Policy')).toBe(EXPECTED_HEADERS['Permissions-Policy']);
	});

	it('page response — includes all expected security headers', async () => {
		const response = await handle({ event: mockRequestEvent('/'), resolve: mockResolve() });
		for (const [header, value] of Object.entries(EXPECTED_HEADERS)) {
			expect(response.headers.get(header)).toBe(value);
		}
	});

	// ── HSTS in production ──────────────────────────────────────────

	it('production mode — includes HSTS header', async () => {
		const response = await handle({ event: mockRequestEvent('/'), resolve: mockResolve() });
		expect(response.headers.get('Strict-Transport-Security')).toBe(HSTS_VALUE);
	});

	// ── API proxy bypass ────────────────────────────────────────────

	it('/api route — does not add security headers', async () => {
		const resolve = mockResolve();
		const response = await handle({ event: mockRequestEvent('/api/v1/users'), resolve });
		expect(response.headers.get('X-Content-Type-Options')).toBeNull();
		expect(response.headers.get('X-Frame-Options')).toBeNull();
		expect(response.headers.get('Referrer-Policy')).toBeNull();
		expect(response.headers.get('Permissions-Policy')).toBeNull();
		expect(response.headers.get('Strict-Transport-Security')).toBeNull();
	});

	it('/api root — does not add security headers', async () => {
		const resolve = mockResolve();
		const response = await handle({ event: mockRequestEvent('/api'), resolve });
		expect(response.headers.get('X-Content-Type-Options')).toBeNull();
	});

	it('/api route — calls resolve directly without paraglide middleware', async () => {
		const resolve = mockResolve();
		await handle({ event: mockRequestEvent('/api/v1/health'), resolve });
		expect(resolve).toHaveBeenCalledOnce();
	});

	// ── Non-API routes still get headers ────────────────────────────

	it('root path — gets security headers', async () => {
		const response = await handle({ event: mockRequestEvent('/'), resolve: mockResolve() });
		expect(response.headers.get('X-Content-Type-Options')).toBe('nosniff');
	});

	it('/login path — gets security headers', async () => {
		const response = await handle({ event: mockRequestEvent('/login'), resolve: mockResolve() });
		expect(response.headers.get('X-Frame-Options')).toBe('DENY');
	});

	it('/admin path — gets security headers', async () => {
		const response = await handle({ event: mockRequestEvent('/admin'), resolve: mockResolve() });
		expect(response.headers.get('Referrer-Policy')).toBe('strict-origin-when-cross-origin');
	});

	// ── Locale handling ─────────────────────────────────────────────

	it('page response — replaces %lang% with locale', async () => {
		const response = await handle({ event: mockRequestEvent('/'), resolve: mockResolve() });
		const html = await response.text();
		expect(html).toContain('lang="en"');
		expect(html).not.toContain('%lang%');
	});

	it('page response — sets locals.locale', async () => {
		const event = mockRequestEvent('/');
		await handle({ event, resolve: mockResolve() });
		expect(event.locals.locale).toBe('en');
	});

	it('page response — sets locals.user to null', async () => {
		const event = mockRequestEvent('/');
		await handle({ event, resolve: mockResolve() });
		expect(event.locals.user).toBeNull();
	});
});

describe('hooks.server handle — dev mode (dev: true)', () => {
	let handle: Handle;

	beforeEach(async () => {
		vi.resetModules();

		vi.doMock('$app/environment', () => ({
			browser: false,
			dev: true,
			building: false,
			version: 'test'
		}));

		vi.doMock('$lib/paraglide/server', () => ({
			paraglideMiddleware: vi.fn(
				(_request: Request, resolve: (args: { locale: string }) => Response | Promise<Response>) =>
					resolve({ locale: 'en' })
			)
		}));

		vi.doMock('$lib/paraglide/runtime', () => ({
			extractLocaleFromHeader: vi.fn(() => null),
			cookieName: 'PARAGLIDE_LOCALE',
			baseLocale: 'en'
		}));

		const mod = await import('./hooks.server');
		handle = mod.handle;
	});

	it('dev mode — does not include HSTS header', async () => {
		const response = await handle({ event: mockRequestEvent('/'), resolve: mockResolve() });
		expect(response.headers.get('Strict-Transport-Security')).toBeNull();
	});

	it('dev mode — still includes other security headers', async () => {
		const response = await handle({ event: mockRequestEvent('/'), resolve: mockResolve() });
		expect(response.headers.get('X-Content-Type-Options')).toBe('nosniff');
		expect(response.headers.get('X-Frame-Options')).toBe('DENY');
		expect(response.headers.get('Referrer-Policy')).toBe('strict-origin-when-cross-origin');
		expect(response.headers.get('Permissions-Policy')).toBe(
			'camera=(), microphone=(), geolocation=()'
		);
	});
});
