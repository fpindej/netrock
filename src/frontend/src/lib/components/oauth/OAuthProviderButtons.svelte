<script lang="ts">
	import { onMount } from 'svelte';
	import { browserClient } from '$lib/api';
	import type { components } from '$lib/api/v1';
	import { Separator } from '$lib/components/ui/separator';
	import { toast } from '$lib/components/ui/sonner';
	import * as m from '$lib/paraglide/messages';
	import OAuthProviderButton from './OAuthProviderButton.svelte';

	type Provider = components['schemas']['ExternalProviderResponse'];

	let providers = $state<Provider[]>([]);
	let loadingProvider = $state<string | null>(null);

	onMount(async () => {
		try {
			const { response, data } = await browserClient.GET('/api/auth/providers');
			if (response.ok && data) {
				providers = data;
			}
		} catch {
			// Silently fail - no OAuth buttons shown
		}
	});

	async function startChallenge(provider: string) {
		if (loadingProvider) return;
		loadingProvider = provider;

		try {
			const redirectUri = `${window.location.origin}/oauth/callback`;
			const { response, data } = await browserClient.POST('/api/auth/external/challenge', {
				body: { provider, redirectUri }
			});

			if (response.ok && data?.authorizationUrl) {
				window.location.href = data.authorizationUrl;
				return;
			}

			toast.error(m.oauth_challengeError());
		} catch {
			toast.error(m.oauth_challengeError());
		} finally {
			loadingProvider = null;
		}
	}
</script>

{#if providers.length > 0}
	<div class="mt-6 space-y-4">
		<div class="relative flex items-center">
			<Separator class="flex-1" />
			<span class="mx-4 shrink-0 text-xs text-muted-foreground">
				{m.oauth_orContinueWith()}
			</span>
			<Separator class="flex-1" />
		</div>
		<div class="grid gap-2">
			{#each providers as provider (provider.name)}
				{@const name = provider.name ?? ''}
				{@const displayName = provider.displayName ?? name}
				<OAuthProviderButton
					provider={name}
					{displayName}
					loading={loadingProvider === name}
					disabled={loadingProvider !== null}
					onclick={() => startChallenge(name)}
				/>
			{/each}
		</div>
	</div>
{/if}
