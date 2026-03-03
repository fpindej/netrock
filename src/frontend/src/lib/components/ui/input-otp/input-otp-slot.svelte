<script lang="ts">
	import { PinInput as InputOTPPrimitive } from 'bits-ui';
	import { cn } from '$lib/utils';

	let {
		ref = $bindable(null),
		cell,
		class: className,
		...restProps
	}: InputOTPPrimitive.CellProps = $props();
</script>

<InputOTPPrimitive.Cell
	{cell}
	bind:ref
	data-slot="input-otp-slot"
	class={cn(
		'relative flex size-10 items-center justify-center border-y border-e border-input text-sm transition-all outline-none first:rounded-s-md first:border-s last:rounded-e-md aria-invalid:border-destructive dark:bg-input/30',
		cell.isActive &&
			'z-10 border-ring ring-[3px] ring-ring/50 ring-offset-background aria-invalid:border-destructive aria-invalid:ring-destructive/20 dark:aria-invalid:ring-destructive/40',
		className
	)}
	{...restProps}
>
	{cell.char}
	{#if cell.hasFakeCaret}
		<div class="pointer-events-none absolute inset-0 flex items-center justify-center">
			<div class="animate-caret-blink h-4 w-px bg-foreground duration-1000"></div>
		</div>
	{/if}
</InputOTPPrimitive.Cell>
