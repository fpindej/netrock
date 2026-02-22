import type { Middleware } from 'openapi-fetch';
import type { paths } from '$lib/api/v1';

/**
 * HTTP methods that are safe to automatically retry after a token refresh.
 *
 * `DELETE` and `PUT` are technically idempotent per RFC 9110 but are excluded
 * intentionally — they mutate server state and automatic retry without
 * explicit user intent could be surprising.
 */
const IDEMPOTENT_METHODS = ['GET', 'HEAD', 'OPTIONS'];

/** The pathname used for cookie-based token refresh, tied to the generated OpenAPI spec. */
const REFRESH_PATHNAME = '/api/auth/refresh' satisfies keyof paths;

/**
 * Creates an openapi-fetch middleware that handles 401 responses by
 * refreshing the access token via cookies and retrying idempotent requests.
 *
 * Works on both browser-side and server-side (SvelteKit load functions).
 * On the browser, `onAuthFailure` typically calls `goto`/`invalidateAll`.
 * On the server, omit `onAuthFailure` — a failed refresh simply passes
 * through the original 401 for the caller to handle.
 *
 * - On 401: triggers a single `POST /api/auth/refresh` (deduplicated across
 *   concurrent requests via a shared promise).
 * - If refresh succeeds: retries the original request for idempotent methods
 *   (GET/HEAD/OPTIONS). Non-idempotent methods (POST/PUT/PATCH/DELETE) return
 *   the original 401 to the caller — the session **is** refreshed, so a
 *   manual retry by the caller will succeed.
 * - If refresh fails: invokes the `onAuthFailure` callback (e.g. to redirect
 *   to login) and returns the original 401.
 *
 * CSRF on the refresh POST is mitigated by the SvelteKit proxy's origin
 * check (`isOriginAllowed` in `+server.ts`) and `SameSite` cookie attributes.
 */
export function createAuthMiddleware(
	fetchFn: typeof fetch,
	baseUrl: string,
	onAuthFailure?: () => void | Promise<void>
): Middleware {
	const refreshUrl = `${baseUrl.replace(/\/$/, '')}${REFRESH_PATHNAME}`;
	let refreshPromise: Promise<Response> | null = null;

	// Guards `onAuthFailure` against concurrent invocations within a single
	// refresh cycle. Reset to `false` each time a new refresh begins.
	// Safety invariant: the check-and-set on this flag is synchronous (no
	// `await` between reading and writing), so JavaScript's single-threaded
	// execution guarantees atomicity.
	let failureHandled = false;

	return {
		async onResponse({ request, response }) {
			if (response.status !== 401) {
				return undefined;
			}

			// Exact pathname match prevents false positives from URLs that
			// merely contain the substring (e.g. `/api/auth/refresh-tokens`).
			const { pathname } = new URL(request.url);
			if (pathname === REFRESH_PATHNAME) {
				return undefined;
			}

			// Deduplicate concurrent refresh calls into a single request.
			if (!refreshPromise) {
				failureHandled = false;
				refreshPromise = fetchFn(refreshUrl, {
					method: 'POST',
					credentials: 'same-origin'
				}).finally(() => {
					refreshPromise = null;
				});
			}

			let refreshResponse: Response;
			try {
				refreshResponse = await refreshPromise;
			} catch {
				if (!failureHandled) {
					failureHandled = true;
					try {
						await onAuthFailure?.();
					} catch {
						// Callback errors must not break the middleware — the caller
						// still needs the original 401 response for error handling.
					}
				}
				return undefined;
			}

			if (!refreshResponse.ok) {
				if (!failureHandled) {
					failureHandled = true;
					try {
						await onAuthFailure?.();
					} catch {
						// Callback errors must not break the middleware.
					}
				}
				return undefined;
			}

			// Only retry idempotent methods — non-idempotent requests (POST, PUT,
			// PATCH, DELETE) could cause double-submission if retried automatically.
			// The session IS refreshed; returning `undefined` passes through the
			// original 401 so the caller can decide whether to retry manually.
			// Note: idempotent methods have no request body, so re-sending the
			// original `Request` object is safe (no consumed-body issue).
			const method = request.method.toUpperCase();
			if (!IDEMPOTENT_METHODS.includes(method)) {
				return undefined;
			}

			// Retry the original request with the new cookies
			return fetchFn(request);
		}
	};
}
