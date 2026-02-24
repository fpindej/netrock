<script lang="ts">
	import { untrack } from 'svelte';
	import * as Dialog from '$lib/components/ui/dialog';
	import * as Avatar from '$lib/components/ui/avatar';
	import { Button } from '$lib/components/ui/button';
	import * as m from '$lib/paraglide/messages';
	import { toast } from '$lib/components/ui/sonner';
	import { invalidateAll } from '$app/navigation';
	import { createCooldown } from '$lib/state';
	import { browserClient, getErrorMessage } from '$lib/api';
	import { Upload } from '@lucide/svelte';

	const ALLOWED_TYPES = ['image/jpeg', 'image/png', 'image/webp', 'image/gif'];
	const MAX_SIZE = 5 * 1024 * 1024; // 5 MB

	interface Props {
		open: boolean;
		hasAvatar: boolean | undefined;
		displayName: string;
		initials: string;
	}

	let { open = $bindable(), hasAvatar, displayName, initials }: Props = $props();

	let selectedFile: File | null = $state(null);
	let previewUrl: string | null = $state(null);
	let fileError = $state('');
	let isLoading = $state(false);
	let isDragOver = $state(false);
	const cooldown = createCooldown();

	let fileInput: HTMLInputElement | undefined = $state();

	// Reset state when dialog opens — untrack inner reads so only `open` is a dependency
	$effect(() => {
		if (open) {
			untrack(() => {
				selectedFile = null;
				if (previewUrl) URL.revokeObjectURL(previewUrl);
				previewUrl = null;
				fileError = '';
			});
		}
	});

	// Clean up object URL when component unmounts or previewUrl changes
	$effect(() => {
		const url = previewUrl;
		return () => {
			if (url) {
				URL.revokeObjectURL(url);
			}
		};
	});

	function validateFile(file: File): string | null {
		if (file.size > MAX_SIZE) return m.profile_avatar_fileTooLarge();
		if (!ALLOWED_TYPES.includes(file.type)) return m.profile_avatar_unsupportedFormat();
		return null;
	}

	function handleFileSelect(file: File) {
		const error = validateFile(file);
		if (error) {
			fileError = error;
			selectedFile = null;
			previewUrl = null;
			return;
		}

		fileError = '';
		selectedFile = file;
		if (previewUrl) URL.revokeObjectURL(previewUrl);
		previewUrl = URL.createObjectURL(file);
	}

	function handleInputChange(e: Event) {
		const input = e.currentTarget as HTMLInputElement;
		const file = input.files?.[0];
		if (file) handleFileSelect(file);
	}

	function handleDrop(e: DragEvent) {
		e.preventDefault();
		isDragOver = false;
		const file = e.dataTransfer?.files[0];
		if (file) handleFileSelect(file);
	}

	function handleDragOver(e: DragEvent) {
		e.preventDefault();
		isDragOver = true;
	}

	function handleDragLeave() {
		isDragOver = false;
	}

	async function handleUpload() {
		if (!selectedFile) return;
		isLoading = true;

		try {
			const file = selectedFile;
			const { response, error } = await browserClient.PUT('/api/users/me/avatar', {
				// @ts-expect-error openapi-fetch types IFormFile as string, but the runtime needs a Blob/File for multipart
				body: { File: file },
				bodySerializer() {
					const fd = new FormData();
					fd.append('File', file);
					return fd;
				}
			});

			if (response.ok) {
				selectedFile = null;
				previewUrl = null;
				if (fileInput) fileInput.value = '';
				toast.success(m.profile_avatar_updateSuccess());
				open = false;
				await invalidateAll();
			} else {
				const msg = getErrorMessage(error, '');
				toast.error(m.profile_avatar_updateError(), msg ? { description: msg } : undefined);
			}
		} catch {
			toast.error(m.profile_avatar_updateError());
		} finally {
			isLoading = false;
		}
	}

	async function handleRemove() {
		isLoading = true;

		try {
			const { response, error } = await browserClient.DELETE('/api/users/me/avatar');

			if (response.ok) {
				toast.success(m.profile_avatar_removeSuccess());
				open = false;
				await invalidateAll();
			} else {
				const msg = getErrorMessage(error, '');
				toast.error(m.profile_avatar_removeError(), msg ? { description: msg } : undefined);
			}
		} catch {
			toast.error(m.profile_avatar_removeError());
		} finally {
			isLoading = false;
		}
	}
</script>

<Dialog.Root bind:open>
	<Dialog.Trigger>
		{#snippet child({ props })}
			<Button {...props} variant="outline" size="sm" class="mt-2 w-full sm:w-auto">
				{m.profile_avatar_change()}
			</Button>
		{/snippet}
	</Dialog.Trigger>
	<Dialog.Content class="sm:max-w-md">
		<Dialog.Header>
			<Dialog.Title>{m.profile_avatar_dialogTitle()}</Dialog.Title>
			<Dialog.Description>
				{m.profile_avatar_dialogDescription()}
			</Dialog.Description>
		</Dialog.Header>
		<div class="grid gap-4 py-4">
			<!-- Preview -->
			<div class="flex justify-center">
				<Avatar.Root class="h-24 w-24">
					{#if previewUrl}
						<Avatar.Image src={previewUrl} alt={displayName} />
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
				{:else if selectedFile}
					<span class="text-foreground">{selectedFile.name}</span>
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
					<Button
						variant="destructive"
						onclick={handleRemove}
						disabled={isLoading || cooldown.active}
					>
						{cooldown.active
							? m.common_waitSeconds({ seconds: cooldown.remaining })
							: m.profile_avatar_remove()}
					</Button>
				{/if}
			</div>
			<div class="flex gap-2">
				<Dialog.Close>
					{#snippet child({ props })}
						<Button {...props} variant="outline">
							{m.common_cancel()}
						</Button>
					{/snippet}
				</Dialog.Close>
				<Button
					onclick={handleUpload}
					disabled={isLoading || !selectedFile || !!fileError || cooldown.active}
				>
					{#if isLoading}
						{m.profile_avatar_uploading()}
					{:else if cooldown.active}
						{m.common_waitSeconds({ seconds: cooldown.remaining })}
					{:else}
						{m.profile_avatar_save()}
					{/if}
				</Button>
			</div>
		</Dialog.Footer>
	</Dialog.Content>
</Dialog.Root>
