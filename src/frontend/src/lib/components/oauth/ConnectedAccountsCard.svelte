<script lang="ts">
	import { onMount } from 'svelte';
	import type { components } from '$lib/api/v1';
	import * as Card from '$lib/components/ui/card';
	import { Button } from '$lib/components/ui/button';
	import { Badge } from '$lib/components/ui/badge';
	import { browserClient } from '$lib/api';
	import * as m from '$lib/paraglide/messages';
	import { startOAuthChallenge } from '$lib/utils/oauth';
	import { Loader2 } from '@lucide/svelte';
	import ProviderIcon from './ProviderIcon.svelte';
	import DisconnectDialog from './DisconnectDialog.svelte';

	type Provider = components['schemas']['ExternalProviderResponse'];

	interface Props {
		linkedProviders: string[];
		hasPassword: boolean;
	}

	let { linkedProviders = $bindable(), hasPassword }: Props = $props();

	let availableProviders = $state<Provider[]>([]);
	let loadingProvider = $state<string | null>(null);
	let disconnectProvider = $state<Provider | null>(null);
	let disconnectDialogOpen = $state(false);

	let canDisconnect = $derived(linkedProviders.length > 1 || hasPassword);

	onMount(async () => {
		try {
			const { response, data } = await browserClient.GET('/api/auth/providers');
			if (response.ok && data) {
				availableProviders = data;
			}
		} catch {
			// Silently fail - no providers shown
		}
	});

	function isLinked(providerName: string | undefined): boolean {
		return !!providerName && linkedProviders.includes(providerName);
	}

	async function handleConnect(provider: string) {
		if (loadingProvider) return;
		loadingProvider = provider;

		const navigating = await startOAuthChallenge(provider);
		if (!navigating) {
			loadingProvider = null;
		}
	}

	function openDisconnectDialog(provider: Provider) {
		disconnectProvider = provider;
		disconnectDialogOpen = true;
	}

	function handleDisconnected() {
		if (disconnectProvider?.name) {
			linkedProviders = linkedProviders.filter((p) => p !== disconnectProvider!.name);
		}
	}
</script>

{#if availableProviders.length > 0}
	<Card.Root class="card-hover">
		<Card.Header>
			<Card.Title>{m.settings_oauth_title()}</Card.Title>
			<Card.Description>{m.settings_oauth_description()}</Card.Description>
		</Card.Header>
		<Card.Content>
			<div class="space-y-3">
				{#each availableProviders as provider (provider.name)}
					<div
						class="flex flex-col gap-3 rounded-lg border p-3 sm:flex-row sm:items-center sm:justify-between"
					>
						<div class="flex items-center gap-3">
							<ProviderIcon provider={provider.name ?? ''} class="h-5 w-5 shrink-0" />
							<div>
								<p class="text-sm font-medium">{provider.displayName ?? provider.name}</p>
								{#if isLinked(provider.name)}
									<Badge variant="default" class="mt-1">
										{m.settings_oauth_connected()}
									</Badge>
								{/if}
							</div>
						</div>
						<div class="flex flex-col gap-2 sm:flex-row sm:justify-end">
							{#if isLinked(provider.name)}
								<Button
									variant="outline"
									class="w-full sm:w-auto"
									disabled={!canDisconnect || loadingProvider !== null}
									onclick={() => openDisconnectDialog(provider)}
								>
									{m.settings_oauth_disconnect()}
								</Button>
							{:else}
								<Button
									variant="outline"
									class="w-full sm:w-auto"
									disabled={loadingProvider !== null}
									onclick={() => handleConnect(provider.name ?? '')}
								>
									{#if loadingProvider === provider.name}
										<Loader2 class="me-2 h-4 w-4 animate-spin" />
									{/if}
									{m.settings_oauth_connect()}
								</Button>
							{/if}
						</div>
					</div>
				{/each}
				{#if !canDisconnect}
					<p class="text-xs text-muted-foreground">
						{m.settings_oauth_setPasswordHint()}
					</p>
				{/if}
			</div>
		</Card.Content>
	</Card.Root>
{/if}

{#if disconnectProvider}
	<DisconnectDialog
		bind:open={disconnectDialogOpen}
		provider={disconnectProvider.name ?? ''}
		displayName={disconnectProvider.displayName ?? disconnectProvider.name ?? ''}
		onDisconnected={handleDisconnected}
	/>
{/if}
