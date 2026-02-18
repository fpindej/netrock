<script lang="ts">
	import type { Snippet } from 'svelte';
	import type { HTMLAttributes } from 'svelte/elements';
	import { cn } from '$lib/utils';

	type Variant = 'default' | 'success' | 'destructive' | 'warning';

	interface Props extends HTMLAttributes<HTMLDivElement> {
		children: Snippet;
		variant?: Variant;
		isLast?: boolean;
		class?: string;
	}

	let {
		children,
		variant = 'default',
		isLast = false,
		class: className,
		...restProps
	}: Props = $props();

	const dotColor: Record<Variant, string> = {
		default: 'bg-primary',
		success: 'bg-success',
		destructive: 'bg-destructive',
		warning: 'bg-warning'
	};
</script>

<div class={cn('relative ps-8', !isLast && 'pb-6', className)} {...restProps}>
	<!-- Vertical line -->
	{#if !isLast}
		<div class="absolute start-[7px] top-3 bottom-0 w-px bg-border"></div>
	{/if}

	<!-- Dot -->
	<div
		class={cn(
			'absolute start-0 top-1.5 h-[15px] w-[15px] rounded-full border-2 border-background ring-2 ring-background',
			dotColor[variant]
		)}
	></div>

	{@render children()}
</div>
