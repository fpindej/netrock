import createClient from 'openapi-fetch';
import type { Middleware } from 'openapi-fetch';
import type { paths } from './v1';

/**
 * Creates a typed openapi-fetch client.
 *
 * The client is middleware-agnostic — callers inject behaviour (auth, logging,
 * etc.) via the `middleware` array. Server-side load functions typically pass
 * no middleware; the browser client has auth middleware wired in the root layout.
 *
 * @param customFetch - Custom fetch function (use SvelteKit's `fetch` in server load functions)
 * @param baseUrl - Base URL for API requests (set to `url.origin` in server load functions)
 * @param middleware - openapi-fetch middleware to apply (e.g. auth refresh)
 */
export function createApiClient(
	customFetch?: typeof fetch,
	baseUrl: string = '',
	middleware: Middleware[] = []
) {
	const client = createClient<paths>({ baseUrl, fetch: customFetch ?? fetch });
	for (const mw of middleware) {
		client.use(mw);
	}
	return client;
}

/**
 * Singleton client for browser-side usage.
 *
 * Created without middleware — the root layout wires auth middleware
 * via {@link initBrowserAuth} in `onMount`. For server-side usage
 * (load functions), call `createApiClient(fetch, url.origin)` instead.
 */
export const browserClient = createApiClient();

/**
 * Registers auth middleware on the browser client exactly once.
 * Safe to call from `onMount` — subsequent calls (HMR, error recovery
 * remounts) are no-ops. The guard prevents middleware stacking.
 */
let authInitialized = false;
export function initBrowserAuth(middleware: Middleware): void {
	if (authInitialized) return;
	browserClient.use(middleware);
	authInitialized = true;
}
