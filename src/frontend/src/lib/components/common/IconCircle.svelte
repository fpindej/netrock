<script lang="ts">
	import type { Component } from 'svelte';
	import type { IconProps } from '@lucide/svelte';
	import { cn } from '$lib/utils';

	interface Props {
		icon: Component<IconProps>;
		variant: 'success' | 'error' | 'warning';
		size?: 'md' | 'lg';
	}

	let { icon: Icon, variant, size = 'lg' }: Props = $props();

	const styles = {
		success: 'bg-success/10 text-success',
		error: 'bg-destructive/10 text-destructive',
		warning: 'bg-warning/10 text-warning'
	};

	const sizes = {
		md: { container: 'h-12 w-12', icon: 'h-6 w-6' },
		lg: { container: 'h-16 w-16', icon: 'h-8 w-8' }
	};
</script>

<div
	class={cn(
		'flex items-center justify-center rounded-full',
		styles[variant],
		sizes[size].container
	)}
>
	<Icon class={sizes[size].icon} />
</div>
