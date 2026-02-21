import type { Middleware } from 'openapi-fetch';

/** HTTP methods that are safe to automatically retry after a token refresh. */
const IDEMPOTENT_METHODS = ['GET', 'HEAD', 'OPTIONS'];

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
export function createAuthMiddleware(
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
