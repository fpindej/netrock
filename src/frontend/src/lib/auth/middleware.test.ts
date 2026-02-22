import type { MergedOptions } from 'openapi-fetch';
import { describe, expect, it, vi } from 'vitest';
import { createAuthMiddleware } from './middleware';

function mockResponse(status: number): Response {
	return new Response(null, { status });
}

function mockRequest(url: string, method = 'GET'): Request {
	return new Request(url, { method });
}

/** Builds the callback params that `onResponse` expects, stubbing fields the middleware never reads. */
function responseParams(request: Request, response: Response) {
	return {
		request,
		response,
		schemaPath: '',
		params: {},
		id: '',
		options: {} as MergedOptions
	};
}

/** Expected shape of the refresh fetch call. */
const REFRESH_INIT = { method: 'POST', credentials: 'same-origin' };

describe('createAuthMiddleware', () => {
	// ── Pass-through ────────────────────────────────────────────────

	it('non-401 response — returns undefined', async () => {
		const fetchFn = vi.fn<typeof fetch>();
		const mw = createAuthMiddleware(fetchFn, '');

		const result = await mw.onResponse!(
			responseParams(mockRequest('http://localhost/api/users'), mockResponse(200))
		);

		expect(result).toBeUndefined();
		expect(fetchFn).not.toHaveBeenCalled();
	});

	it('401 on refresh endpoint — returns undefined', async () => {
		const fetchFn = vi.fn<typeof fetch>();
		const onAuthFailure = vi.fn();
		const mw = createAuthMiddleware(fetchFn, '', onAuthFailure);

		const result = await mw.onResponse!(
			responseParams(mockRequest('http://localhost/api/auth/refresh'), mockResponse(401))
		);

		expect(result).toBeUndefined();
		expect(fetchFn).not.toHaveBeenCalled();
		expect(onAuthFailure).not.toHaveBeenCalled();
	});

	it('401 on path containing refresh substring — triggers refresh normally', async () => {
		const fetchFn = vi.fn<typeof fetch>();
		fetchFn.mockResolvedValueOnce(mockResponse(200)); // refresh
		fetchFn.mockResolvedValueOnce(mockResponse(200)); // retry

		const mw = createAuthMiddleware(fetchFn, '');

		const result = await mw.onResponse!(
			responseParams(mockRequest('http://localhost/api/auth/refresh-tokens'), mockResponse(401))
		);

		// Must NOT be treated as the refresh endpoint itself
		expect(fetchFn).toHaveBeenCalledTimes(2);
		expect(result).toBeInstanceOf(Response);
	});

	// ── Successful refresh — method routing ─────────────────────────

	it.each(['GET', 'HEAD', 'OPTIONS'])(
		'401 + successful refresh + %s — retries the request',
		async (method) => {
			const retryResponse = mockResponse(200);
			const fetchFn = vi.fn<typeof fetch>();
			fetchFn.mockResolvedValueOnce(mockResponse(200)); // refresh
			fetchFn.mockResolvedValueOnce(retryResponse); // retry

			const mw = createAuthMiddleware(fetchFn, '');

			const originalRequest = mockRequest('http://localhost/api/users', method);
			const result = await mw.onResponse!(responseParams(originalRequest, mockResponse(401)));

			expect(fetchFn).toHaveBeenCalledTimes(2);
			expect(fetchFn).toHaveBeenNthCalledWith(2, originalRequest);
			expect(result).toBe(retryResponse);
		}
	);

	it.each(['POST', 'PUT', 'PATCH', 'DELETE'])(
		'401 + successful refresh + %s — does not retry',
		async (method) => {
			const fetchFn = vi.fn<typeof fetch>();
			fetchFn.mockResolvedValueOnce(mockResponse(200)); // refresh

			const mw = createAuthMiddleware(fetchFn, '');

			const result = await mw.onResponse!(
				responseParams(mockRequest('http://localhost/api/users', method), mockResponse(401))
			);

			expect(fetchFn).toHaveBeenCalledTimes(1);
			expect(result).toBeUndefined();
		}
	);

	// ── Refresh URL construction ────────────────────────────────────

	it('refresh uses baseUrl and retries with original request', async () => {
		const retryResponse = mockResponse(200);
		const fetchFn = vi.fn<typeof fetch>();
		fetchFn.mockResolvedValueOnce(mockResponse(200)); // refresh
		fetchFn.mockResolvedValueOnce(retryResponse); // retry

		const mw = createAuthMiddleware(fetchFn, 'http://localhost');

		const originalRequest = mockRequest('http://localhost/api/users', 'GET');
		const result = await mw.onResponse!(responseParams(originalRequest, mockResponse(401)));

		expect(fetchFn).toHaveBeenNthCalledWith(1, 'http://localhost/api/auth/refresh', REFRESH_INIT);
		expect(fetchFn).toHaveBeenNthCalledWith(2, originalRequest);
		expect(result).toBe(retryResponse);
	});

	it('baseUrl with trailing slash — normalized correctly', async () => {
		const fetchFn = vi.fn<typeof fetch>();
		fetchFn.mockResolvedValueOnce(mockResponse(200)); // refresh
		fetchFn.mockResolvedValueOnce(mockResponse(200)); // retry

		const mw = createAuthMiddleware(fetchFn, 'http://localhost/');

		await mw.onResponse!(
			responseParams(mockRequest('http://localhost/api/users'), mockResponse(401))
		);

		expect(fetchFn).toHaveBeenNthCalledWith(1, 'http://localhost/api/auth/refresh', REFRESH_INIT);
	});

	// ── Refresh failure ─────────────────────────────────────────────

	it('refresh returns non-ok — calls onAuthFailure', async () => {
		const fetchFn = vi.fn<typeof fetch>();
		fetchFn.mockResolvedValueOnce(mockResponse(401)); // refresh fails

		const onAuthFailure = vi.fn();
		const mw = createAuthMiddleware(fetchFn, '', onAuthFailure);

		const result = await mw.onResponse!(
			responseParams(mockRequest('http://localhost/api/users'), mockResponse(401))
		);

		expect(onAuthFailure).toHaveBeenCalledOnce();
		expect(result).toBeUndefined();
	});

	it('refresh network error — calls onAuthFailure', async () => {
		const fetchFn = vi.fn<typeof fetch>();
		fetchFn.mockRejectedValueOnce(new TypeError('Failed to fetch'));

		const onAuthFailure = vi.fn();
		const mw = createAuthMiddleware(fetchFn, '', onAuthFailure);

		const result = await mw.onResponse!(
			responseParams(mockRequest('http://localhost/api/users'), mockResponse(401))
		);

		expect(onAuthFailure).toHaveBeenCalledOnce();
		expect(result).toBeUndefined();
	});

	it('onAuthFailure not provided — does not throw', async () => {
		const fetchFn = vi.fn<typeof fetch>();
		fetchFn.mockResolvedValueOnce(mockResponse(401)); // refresh fails

		const mw = createAuthMiddleware(fetchFn, '');

		await expect(
			mw.onResponse!(responseParams(mockRequest('http://localhost/api/users'), mockResponse(401)))
		).resolves.toBeUndefined();
	});

	it('onAuthFailure throws synchronously — does not break middleware', async () => {
		const fetchFn = vi.fn<typeof fetch>();
		fetchFn.mockResolvedValueOnce(mockResponse(401)); // refresh fails

		const onAuthFailure = vi.fn(() => {
			throw new Error('callback exploded');
		});
		const mw = createAuthMiddleware(fetchFn, '', onAuthFailure);

		const result = await mw.onResponse!(
			responseParams(mockRequest('http://localhost/api/users'), mockResponse(401))
		);

		expect(onAuthFailure).toHaveBeenCalledOnce();
		expect(result).toBeUndefined();
	});

	it('onAuthFailure rejects — does not break middleware', async () => {
		const fetchFn = vi.fn<typeof fetch>();
		fetchFn.mockRejectedValueOnce(new TypeError('Failed to fetch')); // refresh network error

		const onAuthFailure = vi.fn().mockRejectedValueOnce(new Error('async failure'));
		const mw = createAuthMiddleware(fetchFn, '', onAuthFailure);

		const result = await mw.onResponse!(
			responseParams(mockRequest('http://localhost/api/users'), mockResponse(401))
		);

		expect(onAuthFailure).toHaveBeenCalledOnce();
		expect(result).toBeUndefined();
	});

	// ── Deduplication and guards ────────────────────────────────────

	it('concurrent 401s — single refresh request', async () => {
		let resolveRefresh: (value: Response) => void;
		const pendingRefresh = new Promise<Response>((r) => {
			resolveRefresh = r;
		});

		const fetchFn = vi.fn<typeof fetch>();
		fetchFn.mockReturnValueOnce(pendingRefresh); // single refresh (pending)
		fetchFn.mockResolvedValueOnce(mockResponse(200)); // retry for first 401
		fetchFn.mockResolvedValueOnce(mockResponse(200)); // retry for second 401

		const mw = createAuthMiddleware(fetchFn, '');

		// Fire both 401s while refresh is still pending
		const p1 = mw.onResponse!(
			responseParams(mockRequest('http://localhost/api/a'), mockResponse(401))
		);
		const p2 = mw.onResponse!(
			responseParams(mockRequest('http://localhost/api/b'), mockResponse(401))
		);

		// Only one refresh call so far
		expect(fetchFn).toHaveBeenCalledTimes(1);
		expect(fetchFn).toHaveBeenNthCalledWith(1, '/api/auth/refresh', REFRESH_INIT);

		// Resolve the refresh
		resolveRefresh!(mockResponse(200));
		const [r1, r2] = await Promise.all([p1, p2]);

		// One refresh + two retries = 3 calls total
		expect(fetchFn).toHaveBeenCalledTimes(3);
		expect(r1).toBeInstanceOf(Response);
		expect(r2).toBeInstanceOf(Response);
	});

	it('onAuthFailure called only once across multiple failures', async () => {
		let resolveRefresh: (value: Response) => void;
		const refreshPromise = new Promise<Response>((r) => {
			resolveRefresh = r;
		});

		const fetchFn = vi.fn<typeof fetch>();
		fetchFn.mockReturnValueOnce(refreshPromise);

		const onAuthFailure = vi.fn();
		const mw = createAuthMiddleware(fetchFn, '', onAuthFailure);

		// Fire two 401s concurrently before refresh resolves
		const p1 = mw.onResponse!(
			responseParams(mockRequest('http://localhost/api/a'), mockResponse(401))
		);
		const p2 = mw.onResponse!(
			responseParams(mockRequest('http://localhost/api/b'), mockResponse(401))
		);

		// Refresh fails
		resolveRefresh!(mockResponse(401));
		await Promise.all([p1, p2]);

		expect(onAuthFailure).toHaveBeenCalledOnce();
	});

	it('second 401 after completed refresh cycle — triggers new refresh', async () => {
		const fetchFn = vi.fn<typeof fetch>();
		// First cycle: refresh + retry
		fetchFn.mockResolvedValueOnce(mockResponse(200));
		fetchFn.mockResolvedValueOnce(mockResponse(200));

		const mw = createAuthMiddleware(fetchFn, '');

		// First 401 — triggers first refresh cycle
		await mw.onResponse!(responseParams(mockRequest('http://localhost/api/a'), mockResponse(401)));
		expect(fetchFn).toHaveBeenCalledTimes(2);

		// Second cycle: new refresh + retry
		fetchFn.mockResolvedValueOnce(mockResponse(200));
		fetchFn.mockResolvedValueOnce(mockResponse(200));

		const result = await mw.onResponse!(
			responseParams(mockRequest('http://localhost/api/b'), mockResponse(401))
		);

		expect(fetchFn).toHaveBeenCalledTimes(4);
		expect(result).toBeInstanceOf(Response);
	});
});
