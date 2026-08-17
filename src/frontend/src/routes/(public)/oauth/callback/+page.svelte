<script lang="ts">
	import { Button } from '$lib/components/ui/button';
	import { resolve } from '$app/paths';
	import { routes } from '$lib/config';
	import * as m from '$lib/paraglide/messages';
	import { Loader2, CircleAlert } from '@lucide/svelte';
	import { IconCircle } from '$lib/components/common';
	import { AuthShell } from '$lib/components/auth';
	import type { ErrorMessagesByCode } from '$lib/api';

	let { data } = $props();

	/**
	 * Translated messages keyed by error code. Backend codes come from the `code`
	 * extension of the ProblemDetails response (see ErrorMessages.cs); the rest are
	 * produced locally by the page load. Unmapped codes fall back to the generic description.
	 */
	const ERROR_MESSAGES: ErrorMessagesByCode = {
		provider_denied: m.oauth_callback_providerDenied,
		external_auth_already_linked_to_other_user: m.oauth_callback_alreadyLinked,
		external_auth_email_not_verified: m.oauth_callback_emailNotVerified,
		external_auth_state_expired: m.oauth_callback_stateExpired,
		external_auth_invalid_state: m.oauth_callback_invalidState,
		external_auth_code_exchange_failed: m.oauth_callback_providerError,
		external_auth_provider_error: m.oauth_callback_providerError,
		auth_login_account_locked: m.oauth_callback_accountLocked
	};

	const errorMessage = $derived(
		(data.error && ERROR_MESSAGES[data.error]?.()) ?? m.oauth_callback_errorDescription()
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
			<Button href={resolve(routes.login)} class="w-full">
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
