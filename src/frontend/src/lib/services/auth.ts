import { goto, invalidateAll } from '$app/navigation';
import { base } from '$app/paths';
import { browserClient } from '$lib/api/client';

export async function logout() {
	await browserClient.POST('/api/auth/logout');
	await invalidateAll();
	// eslint-disable-next-line svelte/no-navigation-without-resolve
	await goto(`${base}/login`);
}
