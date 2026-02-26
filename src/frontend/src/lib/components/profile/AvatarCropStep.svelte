<script lang="ts">
	import Cropper from 'svelte-easy-crop';
	import type { CropArea } from 'svelte-easy-crop';
	import * as Dialog from '$lib/components/ui/dialog';
	import { Button } from '$lib/components/ui/button';
	import * as m from '$lib/paraglide/messages';

	interface Props {
		previewUrl: string;
		crop: { x: number; y: number };
		zoom: number;
		isLoading: boolean;
		cooldownActive: boolean;
		cooldownRemaining: number;
		pixelCrop: CropArea | null;
		oncropcomplete: (pixels: CropArea) => void;
		onback: () => void;
		onupload: () => void;
	}

	let {
		previewUrl,
		crop = $bindable(),
		zoom = $bindable(),
		isLoading,
		cooldownActive,
		cooldownRemaining,
		pixelCrop,
		oncropcomplete,
		onback,
		onupload
	}: Props = $props();
</script>

<Dialog.Header>
	<Dialog.Title>{m.profile_avatar_cropTitle()}</Dialog.Title>
	<Dialog.Description>
		{m.profile_avatar_cropDescription()}
	</Dialog.Description>
</Dialog.Header>
<div class="grid gap-4 py-4">
	<!-- Cropper container -->
	<div class="relative mx-auto aspect-square w-full max-w-[300px] overflow-hidden rounded-lg">
		<Cropper
			image={previewUrl}
			bind:crop
			bind:zoom
			aspect={1}
			cropShape="round"
			showGrid={false}
			oncropcomplete={(e) => oncropcomplete(e.pixels)}
		/>
	</div>

	<!-- Zoom slider -->
	<div class="flex items-center gap-3 px-2">
		<label for="zoom-slider" class="text-sm whitespace-nowrap text-muted-foreground">
			{m.profile_avatar_zoom()}
		</label>
		<input
			id="zoom-slider"
			type="range"
			min="1"
			max="3"
			step="0.01"
			bind:value={zoom}
			class="h-2 w-full cursor-pointer appearance-none rounded-lg bg-muted accent-primary"
		/>
	</div>
</div>
<Dialog.Footer class="flex-col gap-2 sm:flex-row sm:justify-between">
	<Button variant="outline" onclick={onback} disabled={isLoading}>
		{m.profile_avatar_back()}
	</Button>
	<Button onclick={onupload} disabled={isLoading || !pixelCrop || cooldownActive}>
		{#if isLoading}
			{m.profile_avatar_uploading()}
		{:else if cooldownActive}
			{m.common_waitSeconds({ seconds: cooldownRemaining })}
		{:else}
			{m.profile_avatar_saveCrop()}
		{/if}
	</Button>
</Dialog.Footer>
