<script lang="ts">
	import { Button } from '$lib/components/ui/button';
	import { Loader2 } from '@lucide/svelte';
	import * as m from '$lib/paraglide/messages';
	import ProviderIcon from './ProviderIcon.svelte';

	interface Props {
		provider: string;
		displayName: string;
		disabled?: boolean;
		loading?: boolean;
		onclick?: () => void;
	}

	let { provider, displayName, disabled = false, loading = false, onclick }: Props = $props();
</script>

<Button variant="outline" class="w-full" {disabled} {onclick}>
	{#if loading}
		<Loader2 class="me-2 h-4 w-4 animate-spin" />
	{:else}
		<ProviderIcon {provider} class="me-2 h-4 w-4" />
	{/if}
	{m.oauth_continueWith({ provider: displayName })}
</Button>
