<script lang="ts">
	import type { Snippet } from 'svelte';
	import { Separator } from '$lib/components/ui/separator';

	interface Props {
		title: string;
		description?: string;
		/** Optional actions rendered to the right of the title on larger screens */
		actions?: Snippet;
	}

	let { title, description, actions }: Props = $props();
</script>

<div>
	{#if actions}
		<div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
			<div>
				<h3 class="text-lg font-medium">{title}</h3>
				{#if description}
					<p class="text-sm text-muted-foreground">{description}</p>
				{/if}
			</div>
			{@render actions()}
		</div>
	{:else}
		<h3 class="text-lg font-medium">{title}</h3>
		{#if description}
			<p class="text-sm text-muted-foreground">{description}</p>
		{/if}
	{/if}
</div>
<Separator />
