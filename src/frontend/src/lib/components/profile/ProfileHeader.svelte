<script lang="ts">
	import * as Avatar from '$lib/components/ui/avatar';
	import { Button } from '$lib/components/ui/button';
	import { AvatarDialog } from '$lib/components/profile';
	import type { User } from '$lib/types';
	import * as m from '$lib/paraglide/messages';
	import { Camera } from '@lucide/svelte';

	interface Props {
		user: User | null | undefined;
	}

	let { user }: Props = $props();

	let avatarDialogOpen = $state(false);
	let avatarVersion = $state(Date.now());

	// Computed display name
	const displayName = $derived.by(() => {
		const first = user?.firstName;
		const last = user?.lastName;
		if (first || last) {
			return [first, last].filter(Boolean).join(' ');
		}
		return user?.username ?? m.common_user();
	});

	// Avatar URL with cache-busting (version bumps after upload/remove via invalidateAll)
	const avatarUrl = $derived(
		user?.hasAvatar && user?.id ? `/api/users/${user.id}/avatar?v=${avatarVersion}` : null
	);

	// Bump version when the dialog closes (avatar may have changed)
	$effect(() => {
		if (!avatarDialogOpen) {
			avatarVersion = Date.now();
		}
	});

	// Computed initials for avatar
	const initials = $derived.by(() => {
		const first = user?.firstName;
		const last = user?.lastName;
		if (first && last) {
			return `${first[0]}${last[0]}`.toUpperCase();
		}
		if (first) {
			return first.substring(0, 2).toUpperCase();
		}
		return user?.username?.substring(0, 2).toUpperCase() ?? 'ME';
	});
</script>

<div class="flex flex-col items-center gap-4 sm:flex-row">
	<button
		type="button"
		class="group relative h-24 w-24 cursor-pointer rounded-full focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:outline-none"
		aria-label={m.profile_avatar_change()}
		onclick={() => (avatarDialogOpen = true)}
	>
		<!-- Subtle glow on hover -->
		<div
			class="absolute inset-0 rounded-full bg-primary/20 opacity-0 blur-xl transition-opacity duration-300 group-hover:opacity-100"
		></div>
		<Avatar.Root
			class="relative h-24 w-24 ring-2 ring-border transition-all group-hover:ring-primary/50"
		>
			{#if avatarUrl}
				<Avatar.Image src={avatarUrl} alt={displayName} />
			{/if}
			<Avatar.Fallback class="text-lg">
				{initials}
			</Avatar.Fallback>
		</Avatar.Root>
		<!-- Camera overlay on hover -->
		<div
			class="absolute inset-0 flex items-center justify-center rounded-full bg-black/50 opacity-0 transition-opacity duration-200 group-hover:opacity-100"
		>
			<Camera class="text-white" size={24} />
		</div>
	</button>
	<div class="flex flex-col gap-1 text-center sm:text-start">
		<h3 class="text-lg font-medium">{displayName}</h3>
		<p class="text-sm text-muted-foreground">{user?.email ?? ''}</p>
		<Button
			variant="outline"
			size="sm"
			class="mt-2 w-full sm:w-auto"
			onclick={() => (avatarDialogOpen = true)}
		>
			{m.profile_avatar_change()}
		</Button>
		<AvatarDialog
			bind:open={avatarDialogOpen}
			hasAvatar={user?.hasAvatar}
			{avatarUrl}
			{displayName}
			{initials}
		/>
	</div>
</div>
