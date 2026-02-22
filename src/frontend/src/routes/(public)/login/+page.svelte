<script lang="ts">
	import { onMount, tick } from 'svelte';
	import { replaceState } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { LoginForm } from '$lib/components/auth';
	import { toast } from '$lib/components/ui/sonner';
	import * as m from '$lib/paraglide/messages';

	let { data } = $props();

	onMount(async () => {
		if (!data.reason) return;

		// Defer one tick so the Toaster portal (rendered in the root layout)
		// is fully initialised after hydration. Without this, toasts fired
		// during the very first mount cycle of a hard navigation are lost.
		await tick();

		if (data.reason === 'session_expired') {
			toast.error(m.auth_sessionExpired_title(), {
				description: m.auth_sessionExpired_description()
			});
		} else if (data.reason === 'password_changed') {
			toast.success(m.auth_passwordChanged_title(), {
				description: m.auth_passwordChanged_description()
			});
		}

		// Clean URL so bookmarking or refreshing won't re-show the toast
		replaceState(resolve('/login'), {});
	});
</script>

<svelte:head>
	<title>{m.meta_titleTemplate({ title: m.meta_login_title() })}</title>
	<meta name="description" content={m.meta_login_description()} />
</svelte:head>

<LoginForm apiUrl={data.apiUrl} turnstileSiteKey={data.turnstileSiteKey} />
