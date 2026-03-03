<script lang="ts">
	import { Button } from '$lib/components/ui/button';
	import { resolve } from '$app/paths';
	import * as m from '$lib/paraglide/messages';
	import { Loader2, CircleAlert } from '@lucide/svelte';
	import { IconCircle } from '$lib/components/common';
	import { AuthShell } from '$lib/components/auth';

	let { data } = $props();

	/**
	 * Maps backend ProblemDetails `detail` strings to translated messages.
	 * Keys are the exact English strings from ErrorMessages.cs.
	 * Unmapped errors fall back to the generic description.
	 *
	 * TODO: Remove this map once the backend returns error codes instead of
	 * English strings, and use those codes as i18n keys directly.
	 */
	const ERROR_MAP: Record<string, () => string> = {
		provider_denied: () => m.oauth_callback_providerDenied(),
		'This external account is already linked to another user.': () =>
			m.oauth_callback_alreadyLinked(),
		'Your email address must be verified before linking an external account. Please verify your email first.':
			() => m.oauth_callback_emailNotVerified(),
		'OAuth state token has expired. Please try again.': () => m.oauth_callback_stateExpired(),
		'Account is temporarily locked. Please try again later or contact an administrator.': () =>
			m.oauth_callback_accountLocked()
	};

	const errorMessage = $derived(
		(data.error && ERROR_MAP[data.error]?.()) ?? m.oauth_callback_errorDescription()
	);
</script>

<svelte:head>
	<title>{m.meta_titleTemplate({ title: m.meta_login_title() })}</title>
</svelte:head>

<AuthShell>
	{#if data.error}
		<div class="flex flex-col items-center gap-4 py-4">
			<IconCircle icon={CircleAlert} variant="error" size="md" />
			<div class="flex flex-col items-center gap-2 text-center">
				<h1 class="text-xl font-bold">{m.oauth_callback_errorTitle()}</h1>
				<p class="text-sm text-balance text-muted-foreground">
					{errorMessage}
				</p>
			</div>
			<Button href={resolve('/login')} class="w-full">
				{m.oauth_callback_backToLogin()}
			</Button>
		</div>
	{:else}
		<div class="flex flex-col items-center gap-4 py-8">
			<Loader2 class="h-8 w-8 animate-spin text-muted-foreground" />
			<p class="text-sm text-muted-foreground">{m.oauth_callback_processing()}</p>
		</div>
	{/if}
</AuthShell>
