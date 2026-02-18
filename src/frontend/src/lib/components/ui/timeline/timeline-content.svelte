<script lang="ts">
	import type { Snippet } from 'svelte';
	import type { HTMLAttributes } from 'svelte/elements';
	import { cn } from '$lib/utils';

	interface Props extends HTMLAttributes<HTMLDivElement> {
		title: string;
		timestamp: string;
		description?: string;
		children?: Snippet;
		class?: string;
	}

	let { title, timestamp, description, children, class: className, ...restProps }: Props = $props();
</script>

<div class={cn('min-w-0', className)} {...restProps}>
	<div class="flex items-start justify-between gap-2">
		<p class="text-sm leading-tight font-medium">{title}</p>
		<span class="shrink-0 text-xs text-muted-foreground">{timestamp}</span>
	</div>
	{#if description}
		<p class="mt-1 text-xs text-muted-foreground">{description}</p>
	{/if}
	{#if children}
		{@render children()}
	{/if}
</div>
