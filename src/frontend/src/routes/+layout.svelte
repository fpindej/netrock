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
	import { routes } from '$lib/config';
	import { logout, createAuthMiddleware } from '$lib/auth';
	import { initBrowserAuth } from '$lib/api';
	import { initBackendMonitor } from '$lib/api/backend-monitor';
	import { ShortcutsHelp } from '$lib/components/layout';
	import { initHealthCheck } from '$lib/state';
	import { page } from '$app/state';

	let { children } = $props();

	let canonicalUrl = $derived(page.url.origin + page.url.pathname);

	onMount(() => {
		initBackendMonitor();
		initBrowserAuth(
			createAuthMiddleware(fetch, '', async () => {
				toast.error(m.auth_sessionExpired_title(), {
					description: m.auth_sessionExpired_description()
				});
				await invalidateAll();
				await goto(resolve(routes.login));
			})
		);
		const cleanupTheme = initTheme();
		const cleanupHealth = initHealthCheck();
		return () => {
			cleanupTheme?.();
			cleanupHealth?.();
		};
	});

	async function handleSettings() {
		await goto(resolve(routes.settings));
	}
</script>

<svelte:window
	use:globalShortcuts={{
		settings: handleSettings,
		logout: logout
	}}
/>

<ShortcutsHelp />

<svelte:head>
	<title>{m.app_name()}</title>
	<meta name="description" content={m.meta_description()} />
	<link rel="canonical" href={canonicalUrl} />
	<meta property="og:type" content="website" />
	<meta property="og:site_name" content={m.app_name()} />
	<meta property="og:url" content={canonicalUrl} />
	<meta property="og:title" content={m.app_name()} />
	<meta property="og:description" content={m.meta_description()} />
	<meta name="twitter:card" content="summary" />
	<meta name="twitter:title" content={m.app_name()} />
	<meta name="twitter:description" content={m.meta_description()} />
</svelte:head>

<Tooltip.Provider>
	<Toaster />
	{@render children()}
</Tooltip.Provider>
