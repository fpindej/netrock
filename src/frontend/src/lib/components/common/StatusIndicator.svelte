<script lang="ts">
	import type { Snippet, Component } from 'svelte';
	import { cn } from '$lib/utils';

	type Status = 'online' | 'offline' | 'warning';

	interface Props {
		status: Status;
		/** Optional icon component to use instead of dot */
		icon?: Component<{ class?: string }>;
		/** Size of the indicator */
		size?: 'sm' | 'md' | 'lg';
		/** Whether to show the ping animation */
		pulse?: boolean;
		/** Additional classes */
		class?: string;
		/** Optional label snippet */
		children?: Snippet;
	}

	let {
		status,
		icon: Icon,
		size = 'md',
		pulse = true,
		class: className,
		children
	}: Props = $props();

	const statusClass = $derived(
		status === 'online'
			? 'status-online'
			: status === 'offline'
				? 'status-offline'
				: 'status-warning'
	);

	const colorClass = $derived(
		status === 'online'
			? 'text-success'
			: status === 'offline'
				? 'text-destructive'
				: 'text-warning'
	);

	const sizeConfig = {
		sm: { dot: 'h-1.5 w-1.5', icon: 'h-3.5 w-3.5', gap: 'gap-1.5' },
		md: { dot: 'h-2 w-2', icon: 'h-4 w-4', gap: 'gap-2' },
		lg: { dot: 'h-2.5 w-2.5', icon: 'h-5 w-5', gap: 'gap-2.5' }
	};
</script>

<div class={cn('inline-flex items-center', sizeConfig[size].gap, className)}>
	{#if Icon}
		<!-- Icon-based indicator -->
		<div class={cn('status-icon-container', colorClass)}>
			<Icon class={sizeConfig[size].icon} />
			{#if pulse}
				<div class="status-icon-ping">
					<Icon class={cn(sizeConfig[size].icon, 'opacity-50')} />
				</div>
			{/if}
		</div>
	{:else}
		<!-- Dot indicator -->
		<div class={cn('status-indicator', statusClass)}>
			<div class={cn('status-indicator-dot', sizeConfig[size].dot)}></div>
			{#if pulse}
				<div class={cn('status-indicator-ping', sizeConfig[size].dot)}></div>
			{/if}
		</div>
	{/if}

	{#if children}
		{@render children()}
	{/if}
</div>
