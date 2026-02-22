/**
 * Browser-side middleware that detects backend unavailability from API responses.
 *
 * When any `browserClient` request returns 502 or 503, marks the health state
 * as offline and triggers `invalidateAll()` immediately. The (app) layout's
 * server load throws 503, showing the error page with auto-recovery — instead
 * of letting each component show a confusing generic error toast.
 *
 * The proxy returns 503 for ECONNREFUSED and 502 for other connection failures
 * (ETIMEDOUT, EHOSTUNREACH, etc.), so both must be caught.
 *
 * Client-only — never import in `.server.ts` files (pulls in health state
 * which is a client-only singleton).
 */
import { invalidateAll } from '$app/navigation';
import { healthState } from '$lib/state/health.svelte';
import { browserClient } from './client';

let initialized = false;

/**
 * Registers a backend-unavailability middleware on the browser client.
 * Idempotent — safe to call during HMR re-mounts.
 */
export function initBackendMonitor(): void {
	if (initialized) return;
	initialized = true;

	browserClient.use({
		async onResponse({ response }) {
			if (response.status === 502 || response.status === 503) {
				healthState.online = false;
				healthState.checked = true;
				invalidateAll();
			}
			return undefined;
		}
	});
}
