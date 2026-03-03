<script lang="ts">
	import { LoaderCircle } from '@lucide/svelte';
	import { cn } from '$lib/utils';

	interface Props {
		class?: string;
	}

	let { class: className }: Props = $props();
</script>

<div class={cn('flex items-center justify-center py-12', className)}>
	<LoaderCircle class="h-6 w-6 animate-spin text-muted-foreground" />
</div>
