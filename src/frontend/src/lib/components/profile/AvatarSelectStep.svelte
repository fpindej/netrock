<script lang="ts">
	import * as Dialog from '$lib/components/ui/dialog';
	import * as Avatar from '$lib/components/ui/avatar';
	import { Button } from '$lib/components/ui/button';
	import * as m from '$lib/paraglide/messages';
	import { Upload } from '@lucide/svelte';

	interface Props {
		avatarUrl: string | null;
		displayName: string;
		initials: string;
		hasAvatar: boolean | undefined;
		isLoading: boolean;
		fileError: string;
		cooldownActive: boolean;
		cooldownRemaining: number;
		onfileselect: (file: File) => void;
		onremove: () => void;
	}

	let {
		avatarUrl,
		displayName,
		initials,
		hasAvatar,
		isLoading,
		fileError,
		cooldownActive,
		cooldownRemaining,
		onfileselect,
		onremove
	}: Props = $props();

	let isDragOver = $state(false);

	let fileInput: HTMLInputElement | undefined = $state();

	function handleInputChange(e: Event) {
		const input = e.currentTarget as HTMLInputElement;
		const file = input.files?.[0];
		if (file) onfileselect(file);
		// Reset so re-selecting the same file triggers change
		if (input) input.value = '';
	}

	function handleDrop(e: DragEvent) {
		e.preventDefault();
		isDragOver = false;
		const file = e.dataTransfer?.files[0];
		if (file) onfileselect(file);
	}

	function handleDragOver(e: DragEvent) {
		e.preventDefault();
		isDragOver = true;
	}

	function handleDragLeave() {
		isDragOver = false;
	}
</script>

<Dialog.Header>
	<Dialog.Title>{m.profile_avatar_dialogTitle()}</Dialog.Title>
	<Dialog.Description>
		{m.profile_avatar_dialogDescription()}
	</Dialog.Description>
</Dialog.Header>
<div class="grid gap-4 py-4">
	<!-- Current avatar preview -->
	<div class="flex justify-center">
		<Avatar.Root class="h-24 w-24">
			{#if avatarUrl}
				<Avatar.Image src={avatarUrl} alt={displayName} />
			{/if}
			<Avatar.Fallback class="text-lg">
				{initials}
			</Avatar.Fallback>
		</Avatar.Root>
	</div>

	<!-- Dropzone -->
	<button
		type="button"
		class="flex min-h-[120px] cursor-pointer flex-col items-center justify-center gap-2 rounded-lg border-2 border-dashed p-6 text-sm transition-colors {isDragOver
			? 'border-primary bg-primary/5 text-primary'
			: 'border-muted-foreground/25 text-muted-foreground hover:border-primary/50 hover:text-foreground'}"
		ondrop={handleDrop}
		ondragover={handleDragOver}
		ondragleave={handleDragLeave}
		onclick={() => fileInput?.click()}
	>
		<Upload class="opacity-50" size={24} />
		{#if isDragOver}
			<span>{m.profile_avatar_dropzoneActive()}</span>
		{:else}
			<span>{m.profile_avatar_dropzone()}</span>
		{/if}
	</button>

	<input
		bind:this={fileInput}
		type="file"
		accept="image/jpeg,image/png,image/webp,image/gif"
		class="hidden"
		onchange={handleInputChange}
	/>

	{#if fileError}
		<p class="text-xs text-destructive">{fileError}</p>
	{/if}
</div>
<Dialog.Footer class="flex-col gap-2 sm:flex-row sm:justify-between">
	<div>
		{#if hasAvatar}
			<Button variant="destructive" onclick={onremove} disabled={isLoading || cooldownActive}>
				{cooldownActive
					? m.common_waitSeconds({ seconds: cooldownRemaining })
					: m.profile_avatar_remove()}
			</Button>
		{/if}
	</div>
	<Dialog.Close>
		{#snippet child({ props })}
			<Button {...props} variant="outline">
				{m.common_cancel()}
			</Button>
		{/snippet}
	</Dialog.Close>
</Dialog.Footer>
