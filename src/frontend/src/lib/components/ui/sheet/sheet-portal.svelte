<script lang="ts">
	import { Dialog as SheetPrimitive } from 'bits-ui';

	let { children, ...restProps }: SheetPrimitive.PortalProps = $props();
</script>

<SheetPrimitive.Portal {...restProps}>
	{@render children?.()}
</SheetPrimitive.Portal>
