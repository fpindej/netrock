<script lang="ts">
	import '../styles/index.css';
	import 'flag-icons/css/flag-icons.min.css';
	import { onMount } from 'svelte';
	import { initTheme } from '$lib/state/theme.svelte';
	import * as m from '$lib/paraglide/messages';
	import { Toaster, toast } from '$lib/components/ui/sonner';
	import * as Tooltip from '$lib/components/ui/tooltip';
	import { globalShortcuts } from '$lib/state/shortcuts.svelte';
	import { goto, invalidateAll } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { logout, createAuthMiddleware } from '$lib/auth';
	import { initBrowserAuth } from '$lib/api';
	import { ShortcutsHelp } from '$lib/components/layout';
	import { toggleSidebar, initHealthCheck } from '$lib/state';

	let { children } = $props();

	onMount(() => {
		initBrowserAuth(
			createAuthMiddleware(fetch, '', async () => {
				toast.error(m.auth_sessionExpired_title(), {
					description: m.auth_sessionExpired_description()
				});
				await invalidateAll();
				await goto(resolve('/login'));
			})
		);
		const cleanupTheme = initTheme();
		const cleanupHealth = initHealthCheck();
		return () => {
			cleanupTheme?.();
			cleanupHealth();
		};
	});

	async function handleSettings() {
		await goto(resolve('/settings'));
	}
</script>

<svelte:window
	use:globalShortcuts={{
		settings: handleSettings,
		logout: logout,
		toggleSidebar: toggleSidebar
	}}
/>

<ShortcutsHelp />

<svelte:head>
	<title>{m.app_name()}</title>
	<meta name="description" content={m.meta_description()} />
</svelte:head>

<Tooltip.Provider>
	<Toaster />
	{@render children()}
</Tooltip.Provider>
