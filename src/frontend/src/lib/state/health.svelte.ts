/**
 * Global reactive API health state with adaptive polling.
 *
 * Polls the backend health endpoint at different intervals depending on
 * connectivity: fast when offline (to detect recovery quickly), relaxed
 * when online (to avoid unnecessary traffic). Pauses automatically when
 * the browser tab is hidden.
 *
 * Initialize once in the root layout's `onMount`. Any component can read
 * `healthState.online` reactively.
 *
 * Client-only singleton — never import in `.server.ts` files. Module-level
 * state would leak across SSR requests.
 */

const ONLINE_INTERVAL = 30_000;
const OFFLINE_INTERVAL = 5_000;

export const healthState = $state({ online: false, checked: false });

let timer: ReturnType<typeof setTimeout> | null = null;
let visible = true;
let initialized = false;

async function check() {
	if (!initialized) return;
	try {
		const res = await fetch('/api/health');
		healthState.online = res.ok;
	} catch {
		healthState.online = false;
	}
	healthState.checked = true;
	schedule();
}

function schedule() {
	if (timer) clearTimeout(timer);
	if (!visible) return;
	const interval = healthState.online ? ONLINE_INTERVAL : OFFLINE_INTERVAL;
	timer = setTimeout(check, interval);
}

function handleVisibility() {
	visible = document.visibilityState === 'visible';
	if (visible) {
		check();
	} else {
		if (timer) clearTimeout(timer);
		timer = null;
	}
}

/**
 * Start health polling. Call in `onMount`; returns a cleanup function.
 * Idempotent — safe to call during HMR re-mounts.
 */
export function initHealthCheck() {
	if (initialized) return () => {};
	initialized = true;

	check();
	document.addEventListener('visibilitychange', handleVisibility);

	return () => {
		initialized = false;
		if (timer) clearTimeout(timer);
		timer = null;
		document.removeEventListener('visibilitychange', handleVisibility);
	};
}
