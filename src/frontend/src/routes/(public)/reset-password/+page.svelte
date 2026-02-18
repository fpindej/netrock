<script lang="ts">
	import { ResetPasswordForm } from '$lib/components/auth';
	import * as m from '$lib/paraglide/messages';

	let { data } = $props();
</script>

<svelte:head>
	<title
		>{m.meta_titleTemplate({
			title: data.invited ? m.meta_setPassword_title() : m.meta_resetPassword_title()
		})}</title
	>
	<meta
		name="description"
		content={data.invited ? m.meta_setPassword_description() : m.meta_resetPassword_description()}
	/>
</svelte:head>

<ResetPasswordForm token={data.token} invited={data.invited} />
