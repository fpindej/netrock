<script lang="ts">
	interface Props {
		id?: string;
		message?: string;
		class?: string;
	}

	let { id, message, class: className }: Props = $props();
</script>

{#if message}
	<p {id} class="text-xs text-destructive {className ?? ''}">{message}</p>
{/if}
