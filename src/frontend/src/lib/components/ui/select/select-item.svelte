<script lang="ts">
	import { Select as SelectPrimitive } from 'bits-ui';
	import Check from '@lucide/svelte/icons/check';
	import { cn } from '$lib/utils';

	let {
		class: className,
		ref = $bindable(null),
		...restProps
	}: SelectPrimitive.ItemProps = $props();
</script>

<SelectPrimitive.Item
	bind:ref
	class={cn(
		'relative flex w-full cursor-default items-center rounded-sm py-1.5 ps-8 pe-2 text-sm outline-none select-none data-[disabled]:pointer-events-none data-[disabled]:opacity-50 data-[highlighted]:bg-accent data-[highlighted]:text-accent-foreground',
		className
	)}
	{...restProps}
>
	{#snippet children({ selected })}
		<span class="absolute start-2 flex h-3.5 w-3.5 items-center justify-center">
			{#if selected}
				<Check class="h-4 w-4" />
			{/if}
		</span>
		{restProps.label ?? restProps.value}
	{/snippet}
</SelectPrimitive.Item>
