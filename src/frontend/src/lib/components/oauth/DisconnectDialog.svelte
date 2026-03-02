<script lang="ts">
	import * as Dialog from '$lib/components/ui/dialog';
	import { Button } from '$lib/components/ui/button';
	import * as m from '$lib/paraglide/messages';
	import { browserClient, getErrorMessage } from '$lib/api';
	import { toast } from '$lib/components/ui/sonner';
	import { Loader2 } from '@lucide/svelte';

	interface Props {
		open: boolean;
		provider: string;
		displayName: string;
		onDisconnected: () => void;
	}

	let { open = $bindable(), provider, displayName, onDisconnected }: Props = $props();

	let isLoading = $state(false);

	$effect(() => {
		if (open) {
			isLoading = false;
		}
	});

	async function handleDisconnect() {
		isLoading = true;

		try {
			const { response, error: apiError } = await browserClient.POST('/api/auth/external/unlink', {
				body: { provider }
			});

			if (response.ok) {
				open = false;
				toast.success(m.settings_oauth_disconnectSuccess());
				onDisconnected();
			} else {
				toast.error(getErrorMessage(apiError, m.settings_oauth_disconnectError()));
			}
		} catch {
			toast.error(m.settings_oauth_disconnectError());
		} finally {
			isLoading = false;
		}
	}
</script>

<Dialog.Root bind:open>
	<Dialog.Content class="sm:max-w-md">
		<Dialog.Header>
			<Dialog.Title>{m.settings_oauth_disconnectTitle()}</Dialog.Title>
			<Dialog.Description>
				{m.settings_oauth_disconnectDescription({ provider: displayName })}
			</Dialog.Description>
		</Dialog.Header>
		<Dialog.Footer class="flex-col-reverse gap-2 sm:flex-row">
			<Dialog.Close>
				{#snippet child({ props })}
					<Button {...props} variant="outline" disabled={isLoading}>
						{m.common_cancel()}
					</Button>
				{/snippet}
			</Dialog.Close>
			<Button variant="destructive" disabled={isLoading} onclick={handleDisconnect}>
				{#if isLoading}
					<Loader2 class="me-2 h-4 w-4 animate-spin" />
				{/if}
				{m.settings_oauth_disconnectConfirm()}
			</Button>
		</Dialog.Footer>
	</Dialog.Content>
</Dialog.Root>
