<script lang="ts">
	import { Button } from '$lib/components/ui/button';
	import * as Dialog from '$lib/components/ui/dialog';
	import { Separator } from '$lib/components/ui/separator';
	import { browserClient, handleMutationError } from '$lib/api';
	import { toast } from '$lib/components/ui/sonner';
	import { goto, invalidateAll } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { Loader2, Lock, Unlock, Trash2, KeyRound } from '@lucide/svelte';
	import type { AdminUser } from '$lib/types';
	import type { Cooldown } from '$lib/state';
	import * as m from '$lib/paraglide/messages';

	interface Props {
		user: AdminUser;
		canManage: boolean;
		cooldown: Cooldown;
	}

	let { user, canManage, cooldown }: Props = $props();

	let deleteDialogOpen = $state(false);
	let resetDialogOpen = $state(false);
	let isLocking = $state(false);
	let isUnlocking = $state(false);
	let isDeleting = $state(false);
	let isSendingReset = $state(false);

	async function lockUser() {
		isLocking = true;
		const { response, error } = await browserClient.POST('/api/v1/admin/users/{id}/lock', {
			params: { path: { id: user.id ?? '' } }
		});
		isLocking = false;

		if (response.ok) {
			toast.success(m.admin_userDetail_lockSuccess());
			await invalidateAll();
		} else {
			handleMutationError(response, error, {
				cooldown,
				fallback: m.admin_userDetail_lockError()
			});
		}
	}

	async function unlockUser() {
		isUnlocking = true;
		const { response, error } = await browserClient.POST('/api/v1/admin/users/{id}/unlock', {
			params: { path: { id: user.id ?? '' } }
		});
		isUnlocking = false;

		if (response.ok) {
			toast.success(m.admin_userDetail_unlockSuccess());
			await invalidateAll();
		} else {
			handleMutationError(response, error, {
				cooldown,
				fallback: m.admin_userDetail_unlockError()
			});
		}
	}

	async function deleteUser() {
		isDeleting = true;
		const { response, error } = await browserClient.DELETE('/api/v1/admin/users/{id}', {
			params: { path: { id: user.id ?? '' } }
		});
		isDeleting = false;
		deleteDialogOpen = false;

		if (response.ok) {
			toast.success(m.admin_userDetail_deleteSuccess());
			await goto(resolve('/admin/users'));
		} else {
			handleMutationError(response, error, {
				cooldown,
				fallback: m.admin_userDetail_deleteError()
			});
		}
	}

	async function sendPasswordReset() {
		isSendingReset = true;
		const { response, error } = await browserClient.POST(
			'/api/v1/admin/users/{id}/send-password-reset',
			{
				params: { path: { id: user.id ?? '' } }
			}
		);
		isSendingReset = false;
		resetDialogOpen = false;

		if (response.ok) {
			toast.success(m.admin_userDetail_resetSuccess());
		} else {
			handleMutationError(response, error, {
				cooldown,
				fallback: m.admin_userDetail_resetError()
			});
		}
	}
</script>

{#if canManage}
	<Separator class="mt-4" />

	<div class="mt-4 flex flex-wrap items-center gap-2">
		{#if user.isLockedOut}
			<Button
				variant="outline"
				size="default"
				disabled={isUnlocking || cooldown.active}
				onclick={unlockUser}
			>
				{#if cooldown.active}
					{m.common_waitSeconds({ seconds: cooldown.remaining })}
				{:else if isUnlocking}
					<Loader2 class="me-2 h-4 w-4 animate-spin" />
					{m.admin_userDetail_unlockAccount()}
				{:else}
					<Unlock class="me-2 h-4 w-4" />
					{m.admin_userDetail_unlockAccount()}
				{/if}
			</Button>
		{:else}
			<Button
				variant="outline"
				size="default"
				disabled={isLocking || cooldown.active}
				onclick={lockUser}
			>
				{#if cooldown.active}
					{m.common_waitSeconds({ seconds: cooldown.remaining })}
				{:else if isLocking}
					<Loader2 class="me-2 h-4 w-4 animate-spin" />
					{m.admin_userDetail_lockAccount()}
				{:else}
					<Lock class="me-2 h-4 w-4" />
					{m.admin_userDetail_lockAccount()}
				{/if}
			</Button>
		{/if}

		<Dialog.Root bind:open={resetDialogOpen}>
			<Dialog.Trigger>
				{#snippet child({ props })}
					<Button variant="outline" size="default" {...props}>
						<KeyRound class="me-2 h-4 w-4" />
						{m.admin_userDetail_sendPasswordReset()}
					</Button>
				{/snippet}
			</Dialog.Trigger>
			<Dialog.Content>
				<Dialog.Header>
					<Dialog.Title>{m.admin_userDetail_resetConfirmTitle()}</Dialog.Title>
					<Dialog.Description>
						{m.admin_userDetail_resetConfirmDescription()}
					</Dialog.Description>
				</Dialog.Header>
				<Dialog.Footer class="flex-col-reverse sm:flex-row">
					<Button variant="outline" onclick={() => (resetDialogOpen = false)}>
						{m.common_cancel()}
					</Button>
					<Button disabled={isSendingReset || cooldown.active} onclick={sendPasswordReset}>
						{#if cooldown.active}
							{m.common_waitSeconds({ seconds: cooldown.remaining })}
						{:else}
							{#if isSendingReset}
								<Loader2 class="me-2 h-4 w-4 animate-spin" />
							{/if}
							{m.admin_userDetail_sendPasswordReset()}
						{/if}
					</Button>
				</Dialog.Footer>
			</Dialog.Content>
		</Dialog.Root>

		<Dialog.Root bind:open={deleteDialogOpen}>
			<Dialog.Trigger>
				{#snippet child({ props })}
					<Button variant="destructive" size="default" {...props}>
						<Trash2 class="me-2 h-4 w-4" />
						{m.admin_userDetail_deleteAccount()}
					</Button>
				{/snippet}
			</Dialog.Trigger>
			<Dialog.Content>
				<Dialog.Header>
					<Dialog.Title>{m.admin_userDetail_deleteConfirmTitle()}</Dialog.Title>
					<Dialog.Description>
						{m.admin_userDetail_deleteConfirmDescription()}
					</Dialog.Description>
				</Dialog.Header>
				<Dialog.Footer class="flex-col-reverse sm:flex-row">
					<Button variant="outline" onclick={() => (deleteDialogOpen = false)}>
						{m.common_cancel()}
					</Button>
					<Button
						variant="destructive"
						disabled={isDeleting || cooldown.active}
						onclick={deleteUser}
					>
						{#if cooldown.active}
							{m.common_waitSeconds({ seconds: cooldown.remaining })}
						{:else}
							{#if isDeleting}
								<Loader2 class="me-2 h-4 w-4 animate-spin" />
							{/if}
							{m.common_delete()}
						{/if}
					</Button>
				</Dialog.Footer>
			</Dialog.Content>
		</Dialog.Root>
	</div>
{/if}
