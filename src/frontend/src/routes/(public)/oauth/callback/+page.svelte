<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import { Button } from '$lib/components/ui/button';
	import { resolve } from '$app/paths';
	import * as m from '$lib/paraglide/messages';
	import { Loader2, CircleAlert } from '@lucide/svelte';
	import { LoginBackground } from '$lib/components/auth';

	let { data } = $props();
</script>

<svelte:head>
	<title>{m.meta_titleTemplate({ title: m.meta_login_title() })}</title>
</svelte:head>

<LoginBackground>
	{#if data.error}
		<div class="sm:mx-auto sm:w-full sm:max-w-md">
			<Card.Root class="border-muted/60 bg-card/50 shadow-xl backdrop-blur-sm">
				<Card.Header class="text-center">
					<div
						class="mx-auto mb-2 flex h-12 w-12 items-center justify-center rounded-full bg-destructive/10"
					>
						<CircleAlert class="h-6 w-6 text-destructive" />
					</div>
					<Card.Title>{m.oauth_callback_errorTitle()}</Card.Title>
					<Card.Description>
						{data.error === 'provider_denied'
							? m.oauth_callback_providerDenied()
							: m.oauth_callback_errorDescription()}
					</Card.Description>
				</Card.Header>
				<Card.Content>
					<Button href={resolve('/login')} class="w-full">
						{m.oauth_callback_backToLogin()}
					</Button>
				</Card.Content>
			</Card.Root>
		</div>
	{:else}
		<div class="flex flex-col items-center gap-4">
			<Loader2 class="h-8 w-8 animate-spin text-muted-foreground" />
			<p class="text-sm text-muted-foreground">{m.oauth_callback_processing()}</p>
		</div>
	{/if}
</LoginBackground>
