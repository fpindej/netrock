<script lang="ts">
	import { RegisterForm } from '$lib/components/auth';
	import * as m from '$lib/paraglide/messages';

	let { data } = $props();
</script>

<svelte:head>
	<title>{m.meta_titleTemplate({ title: m.meta_register_title() })}</title>
	<meta name="description" content={m.meta_register_description()} />
</svelte:head>

<RegisterForm turnstileSiteKey={data.turnstileSiteKey} />
