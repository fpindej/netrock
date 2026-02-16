<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import * as Dialog from '$lib/components/ui/dialog';
	import { Button } from '$lib/components/ui/button';
	import { Loader2, Trash2 } from '@lucide/svelte';
	import { browserClient, handleMutationError } from '$lib/api';
	import { toast } from '$lib/components/ui/sonner';
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import type { Cooldown } from '$lib/state';
	import * as m from '$lib/paraglide/messages';

	interface Props {
		roleId: string;
		cooldown: Cooldown;
	}

	let { roleId, cooldown }: Props = $props();

	let deleteDialogOpen = $state(false);
	let isDeleting = $state(false);

	async function deleteRole() {
		isDeleting = true;
		const { response, error } = await browserClient.DELETE('/api/v1/admin/roles/{id}', {
			params: { path: { id: roleId } }
		});
		isDeleting = false;
		deleteDialogOpen = false;

		if (response.ok) {
			toast.success(m.admin_roles_deleteSuccess());
			await goto(resolve('/admin/roles'));
		} else {
			handleMutationError(response, error, {
				cooldown,
				fallback: m.admin_roles_deleteError()
			});
		}
	}
</script>

<Card.Root class="border-destructive">
	<Card.Header>
		<Card.Title>{m.common_dangerZone()}</Card.Title>
	</Card.Header>
	<Card.Content>
		<Dialog.Root bind:open={deleteDialogOpen}>
			<Dialog.Trigger>
				{#snippet child({ props })}
					<Button variant="destructive" size="sm" {...props}>
						<Trash2 class="me-2 h-4 w-4" />
						{m.admin_roles_deleteRole()}
					</Button>
				{/snippet}
			</Dialog.Trigger>
			<Dialog.Content>
				<Dialog.Header>
					<Dialog.Title>{m.admin_roles_deleteConfirmTitle()}</Dialog.Title>
					<Dialog.Description>
						{m.admin_roles_deleteConfirmDescription()}
					</Dialog.Description>
				</Dialog.Header>
				<Dialog.Footer class="flex-col-reverse sm:flex-row">
					<Button variant="outline" onclick={() => (deleteDialogOpen = false)}>
						{m.common_cancel()}
					</Button>
					<Button
						variant="destructive"
						disabled={isDeleting || cooldown.active}
						onclick={deleteRole}
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
	</Card.Content>
</Card.Root>
