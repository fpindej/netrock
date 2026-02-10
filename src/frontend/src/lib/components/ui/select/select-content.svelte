<script lang="ts">
	import { Select as SelectPrimitive } from 'bits-ui';
	import { cn } from '$lib/utils';

	let {
		class: className,
		children,
		ref = $bindable(null),
		...restProps
	}: SelectPrimitive.ContentProps = $props();
</script>

<SelectPrimitive.Content
	bind:ref
	class={cn(
		'relative z-50 max-h-96 min-w-[8rem] overflow-hidden rounded-md border bg-popover text-popover-foreground shadow-md data-[side=bottom]:slide-in-from-top-2 data-[side=left]:slide-in-from-right-2 data-[side=right]:slide-in-from-left-2 data-[side=top]:slide-in-from-bottom-2 data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=closed]:zoom-out-95 data-[state=open]:animate-in data-[state=open]:fade-in-0 data-[state=open]:zoom-in-95',
		className
	)}
	{...restProps}
>
	<div class="p-1">
		{@render children?.()}
	</div>
</SelectPrimitive.Content>
