<script lang="ts">
	import { untrack } from 'svelte';
	import type { CropArea } from 'svelte-easy-crop';
	import * as Dialog from '$lib/components/ui/dialog';
	import * as m from '$lib/paraglide/messages';
	import { toast } from '$lib/components/ui/sonner';
	import { invalidateAll } from '$app/navigation';
	import { createCooldown } from '$lib/state';
	import { browserClient, getErrorMessage } from '$lib/api';
	import { getCroppedBlob } from '$lib/utils';
	import AvatarSelectStep from './AvatarSelectStep.svelte';
	import AvatarCropStep from './AvatarCropStep.svelte';

	const ALLOWED_TYPES = ['image/jpeg', 'image/png', 'image/webp', 'image/gif'];
	const MAX_SIZE = 5 * 1024 * 1024; // 5 MB

	type Step = 'select' | 'crop';

	interface Props {
		open: boolean;
		hasAvatar: boolean | undefined;
		avatarUrl: string | null;
		displayName: string;
		initials: string;
	}

	let { open = $bindable(), hasAvatar, avatarUrl, displayName, initials }: Props = $props();

	let step: Step = $state('select');
	let previewUrl: string | null = $state(null);
	let fileError = $state('');
	let isLoading = $state(false);
	let isDragOver = $state(false);
	const cooldown = createCooldown();

	// Crop state
	let crop = $state({ x: 0, y: 0 });
	let zoom = $state(1);
	let pixelCrop: CropArea | null = $state(null);

	// Reset state when dialog opens — untrack inner reads so only `open` is a dependency
	$effect(() => {
		if (open) {
			untrack(() => {
				step = 'select';
				if (previewUrl) URL.revokeObjectURL(previewUrl);
				previewUrl = null;
				fileError = '';
				crop = { x: 0, y: 0 };
				zoom = 1;
				pixelCrop = null;
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
			previewUrl = null;
			return;
		}

		fileError = '';
		if (previewUrl) URL.revokeObjectURL(previewUrl);
		previewUrl = URL.createObjectURL(file);

		// Reset crop state and advance to crop step
		crop = { x: 0, y: 0 };
		zoom = 1;
		pixelCrop = null;
		step = 'crop';
	}

	function handleBack() {
		step = 'select';
		crop = { x: 0, y: 0 };
		zoom = 1;
		pixelCrop = null;
	}

	async function handleUpload() {
		if (!previewUrl || !pixelCrop) return;
		isLoading = true;

		try {
			const blob = await getCroppedBlob(previewUrl, pixelCrop);
			const fd = new FormData();
			fd.append('File', blob, 'avatar.jpg');

			const { response, error } = await browserClient.PUT('/api/users/me/avatar', {
				// @ts-expect-error openapi-fetch types IFormFile as string, but the runtime needs a Blob/File for multipart
				body: { File: blob },
				bodySerializer() {
					return fd;
				}
			});

			if (response.ok) {
				previewUrl = null;
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
	<Dialog.Content class="sm:max-w-md">
		{#if step === 'select'}
			<AvatarSelectStep
				{avatarUrl}
				{displayName}
				{initials}
				{hasAvatar}
				{isLoading}
				bind:isDragOver
				{fileError}
				cooldownActive={cooldown.active}
				cooldownRemaining={cooldown.remaining}
				onfileselect={handleFileSelect}
				onremove={handleRemove}
			/>
		{:else if previewUrl}
			<AvatarCropStep
				{previewUrl}
				bind:crop
				bind:zoom
				{isLoading}
				cooldownActive={cooldown.active}
				cooldownRemaining={cooldown.remaining}
				{pixelCrop}
				oncropcomplete={(pixels) => (pixelCrop = pixels)}
				onback={handleBack}
				onupload={handleUpload}
			/>
		{/if}
	</Dialog.Content>
</Dialog.Root>
