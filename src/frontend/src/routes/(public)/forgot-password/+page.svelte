<script lang="ts">
	import { ForgotPasswordForm } from '$lib/components/auth';
	import * as m from '$lib/paraglide/messages';
</script>

<svelte:head>
	<title>{m.meta_titleTemplate({ title: m.meta_forgotPassword_title() })}</title>
	<meta name="description" content={m.meta_forgotPassword_description()} />
</svelte:head>

<ForgotPasswordForm />
