<script lang="ts">
	import { replaceState } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { LoginForm } from '$lib/components/auth';
	import { toast } from '$lib/components/ui/sonner';
	import * as m from '$lib/paraglide/messages';

	let { data } = $props();

	if (data.sessionExpired) {
		toast.error(m.auth_sessionExpired_title(), {
			description: m.auth_sessionExpired_description()
		});
		// Clean URL so bookmarking or refreshing won't re-show the toast
		replaceState(resolve('/login'), {});
	}
</script>

<svelte:head>
	<title>{m.meta_titleTemplate({ title: m.meta_login_title() })}</title>
	<meta name="description" content={m.meta_login_description()} />
</svelte:head>

<LoginForm apiUrl={data.apiUrl} />
