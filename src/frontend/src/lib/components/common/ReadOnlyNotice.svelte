<script lang="ts">
	import { Lock } from '@lucide/svelte';

	interface Props {
		message: string;
	}

	let { message }: Props = $props();
</script>

<div class="flex items-center gap-2 rounded-md bg-muted px-3 py-2 text-sm text-muted-foreground">
	<Lock class="h-4 w-4 shrink-0" />
	<span>{message}</span>
</div>
