/**
 * Browser-side middleware that detects backend unavailability from API responses.
 *
 * When any `browserClient` request returns 503, marks the health state as
 * offline. The reactive effect in `(app)/+layout.svelte` picks up the
 * transition and triggers `invalidateAll()`, which shows the 503 error page
 * with auto-recovery — instead of letting each component show a confusing
 * generic error toast.
 *
 * Client-only — never import in `.server.ts` files (pulls in health state
 * which is a client-only singleton).
 */
import { healthState } from '$lib/state/health.svelte';
import { browserClient } from './client';

let initialized = false;

/**
 * Registers a 503-detection middleware on the browser client.
 * Idempotent — safe to call during HMR re-mounts.
 */
export function initBackendMonitor(): void {
	if (initialized) return;
	initialized = true;

	browserClient.use({
		async onResponse({ response }) {
			if (response.status === 503) {
				healthState.online = false;
				healthState.checked = true;
			}
			return undefined;
		}
	});
}
