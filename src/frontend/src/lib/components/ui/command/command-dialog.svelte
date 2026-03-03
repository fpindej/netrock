<script lang="ts">
	import type { Command as CommandPrimitive, Dialog as DialogPrimitive } from 'bits-ui';
	import type { Snippet } from 'svelte';
	import Command from './command.svelte';
	import * as Dialog from '$lib/components/ui/dialog';
	import type { WithoutChildrenOrChild } from '$lib/utils';

	let {
		open = $bindable(false),
		ref = $bindable(null),
		value = $bindable(''),
		title,
		description,
		portalProps,
		children,
		...restProps
	}: WithoutChildrenOrChild<DialogPrimitive.RootProps> &
		WithoutChildrenOrChild<CommandPrimitive.RootProps> & {
			portalProps?: DialogPrimitive.PortalProps;
			children: Snippet;
			title: string;
			description: string;
		} = $props();

	$effect(() => {
		if (open) value = '';
	});
</script>

<Dialog.Root bind:open {...restProps}>
	<Dialog.Content class="overflow-hidden p-0" {portalProps}>
		<Dialog.Header class="sr-only">
			<Dialog.Title>{title}</Dialog.Title>
			<Dialog.Description>{description}</Dialog.Description>
		</Dialog.Header>
		<Command
			class="**:data-[slot=command-input-wrapper]:h-12 [&_[data-command-group]]:px-2 [&_[data-command-group]:not([hidden])_~[data-command-group]]:pt-0 [&_[data-command-input-wrapper]_svg]:size-5 [&_[data-command-input]]:h-12 [&_[data-command-item]]:px-2 [&_[data-command-item]]:py-3 [&_[data-command-item]_svg]:size-5"
			{...restProps}
			bind:value
			bind:ref
			{children}
		/>
	</Dialog.Content>
</Dialog.Root>
