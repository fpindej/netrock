<script lang="ts">
	import { ResetPasswordForm } from '$lib/components/auth';
	import * as m from '$lib/paraglide/messages';

	let { data } = $props();
</script>

<svelte:head>
	<title>{m.meta_titleTemplate({ title: m.meta_resetPassword_title() })}</title>
	<meta name="description" content={m.meta_resetPassword_description()} />
</svelte:head>

<ResetPasswordForm email={data.email} token={data.token} />
