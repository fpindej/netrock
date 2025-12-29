<script lang="ts">
	import type { Component, Snippet } from 'svelte';
	import type { IconProps } from '@lucide/svelte';

	let {
		icon: IconComponent,
		label,
		children
	}: {
		icon: Component<IconProps>;
		label: string;
		children: Snippet;
	} = $props();
</script>

<div class="flex items-start gap-4 rounded-md border p-4">
	<IconComponent class="mt-1 h-5 w-5 text-muted-foreground" />
	<div class="grid gap-1">
		<span class="text-sm leading-none font-medium">{label}</span>
		<div class="text-sm break-all text-muted-foreground">
			{@render children()}
		</div>
	</div>
</div>
