<script lang="ts">
	import type { Component } from 'svelte';
	import type { IconProps } from '@lucide/svelte';

	interface Props {
		icon: Component<IconProps>;
		message: string;
	}

	let { icon: Icon, message }: Props = $props();
</script>

<div class="flex flex-col items-center justify-center py-12 text-center">
	<div class="mb-3 rounded-full bg-muted p-3">
		<Icon class="h-6 w-6 text-muted-foreground" />
	</div>
	<p class="text-sm text-muted-foreground">{message}</p>
</div>
