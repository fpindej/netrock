// Pattern documented in src/frontend/AGENTS.md — update both when changing.
import createClient from 'openapi-fetch';
import type { Middleware } from 'openapi-fetch';
import type { paths } from './v1';

/** HTTP methods that are safe to automatically retry after a token refresh. */
const IDEMPOTENT_METHODS = ['GET', 'HEAD', 'OPTIONS'];

/**
 * Module-level auth failure callback, set once from the root layout via
 * {@link setAuthFailureHandler}. When a token refresh fails on the browser
 * client, this is invoked to redirect the user to the login page.
 */
let authFailureHandler: (() => void) | null = null;

/**
 * Registers a callback invoked when token refresh fails on the browser client.
 * Call once from the root layout's `onMount` to wire up navigation.
 */
export function setAuthFailureHandler(handler: () => void): void {
	authFailureHandler = handler;
}

/**
 * Creates an openapi-fetch middleware that handles 401 responses by
 * refreshing the access token via cookies and retrying idempotent requests.
 *
 * - On 401: triggers a single `POST /api/auth/refresh` (deduplicated across
 *   concurrent requests via a shared promise).
 * - If refresh succeeds: retries the original request for idempotent methods
 *   (GET/HEAD/OPTIONS). Non-idempotent methods (POST/PUT/PATCH/DELETE) return
 *   the original 401 to the caller to avoid double-submission.
 * - If refresh fails: invokes the `onAuthFailure` callback (e.g. to redirect
 *   to login) and returns the original 401.
 */
function createAuthMiddleware(
	fetchFn: typeof fetch,
	baseUrl: string,
	onAuthFailure?: () => void
): Middleware {
	let refreshPromise: Promise<Response> | null = null;

	return {
		async onResponse({ request, response }) {
			if (response.status !== 401) {
				return undefined;
			}

			// Never try to refresh the refresh endpoint itself
			if (request.url.includes('/api/auth/refresh')) {
				return undefined;
			}

			// Deduplicate concurrent refresh calls into a single request
			if (!refreshPromise) {
				const refreshUrl = baseUrl ? `${baseUrl}/api/auth/refresh` : '/api/auth/refresh';
				refreshPromise = fetchFn(refreshUrl, { method: 'POST' }).finally(() => {
					refreshPromise = null;
				});
			}

			let refreshResponse: Response;
			try {
				refreshResponse = await refreshPromise;
			} catch {
				onAuthFailure?.();
				return undefined;
			}

			if (!refreshResponse.ok) {
				onAuthFailure?.();
				return undefined;
			}

			// Only retry idempotent methods — non-idempotent requests (POST, PUT,
			// PATCH, DELETE) could cause double-submission if retried automatically.
			const method = request.method.toUpperCase();
			if (!IDEMPOTENT_METHODS.includes(method)) {
				return undefined;
			}

			// Retry the original request with the new cookies
			return fetchFn(request);
		}
	};
}

/**
 * Creates a typed API client with automatic cookie-based token refresh.
 *
 * @param customFetch - Custom fetch function (use SvelteKit's `fetch` in server load functions)
 * @param baseUrl - Base URL for API requests (set to `url.origin` in server load functions)
 * @param onAuthFailure - Called when token refresh fails (e.g. redirect to login)
 */
export const createApiClient = (
	customFetch?: typeof fetch,
	baseUrl: string = '',
	onAuthFailure?: () => void
) => {
	const fetchFn = customFetch ?? fetch;
	const client = createClient<paths>({ baseUrl, fetch: fetchFn });
	client.use(createAuthMiddleware(fetchFn, baseUrl, onAuthFailure));
	return client;
};

/**
 * Client for browser-side usage only.
 * Auth failure handling is wired via {@link setAuthFailureHandler} in the root layout.
 * For server-side (load functions), use createApiClient(fetch, url.origin).
 */
export const browserClient = createApiClient(undefined, '', () => {
	authFailureHandler?.();
});
