<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import { Button } from '$lib/components/ui/button';
	import { RolePermissionEditor } from '$lib/components/admin';
	import { Loader2, Save } from '@lucide/svelte';
	import { browserClient, handleMutationError } from '$lib/api';
	import { toast } from '$lib/components/ui/sonner';
	import { invalidateAll } from '$app/navigation';
	import type { Cooldown } from '$lib/state';
	import type { PermissionGroup } from '$lib/types';
	import * as m from '$lib/paraglide/messages';

	interface Props {
		roleId: string;
		permissionGroups: PermissionGroup[];
		selectedPermissions: string[];
		canEditPermissions: boolean;
		cooldown: Cooldown;
	}

	let {
		roleId,
		permissionGroups,
		selectedPermissions = $bindable(),
		canEditPermissions,
		cooldown
	}: Props = $props();

	let isSavingPermissions = $state(false);

	async function savePermissions() {
		isSavingPermissions = true;
		const { response, error } = await browserClient.PUT('/api/v1/admin/roles/{id}/permissions', {
			params: { path: { id: roleId } },
			body: { permissions: selectedPermissions }
		});
		isSavingPermissions = false;

		if (response.ok) {
			toast.success(m.admin_roles_permissionsSaved());
			await invalidateAll();
		} else {
			handleMutationError(response, error, {
				cooldown,
				fallback: m.admin_roles_permissionsSaveError()
			});
		}
	}
</script>

<Card.Root>
	<Card.Header>
		<Card.Title>{m.admin_roles_permissionsTitle()}</Card.Title>
		<Card.Description>{m.admin_roles_permissionsDescription()}</Card.Description>
	</Card.Header>
	<Card.Content class="space-y-4">
		<RolePermissionEditor
			{permissionGroups}
			selected={selectedPermissions}
			disabled={!canEditPermissions}
			onchange={(perms) => (selectedPermissions = perms)}
		/>
		{#if canEditPermissions}
			<Button size="sm" disabled={isSavingPermissions || cooldown.active} onclick={savePermissions}>
				{#if cooldown.active}
					{m.common_waitSeconds({ seconds: cooldown.remaining })}
				{:else if isSavingPermissions}
					<Loader2 class="me-2 h-4 w-4 animate-spin" />
					{m.admin_roles_savePermissions()}
				{:else}
					<Save class="me-2 h-4 w-4" />
					{m.admin_roles_savePermissions()}
				{/if}
			</Button>
		{/if}
	</Card.Content>
</Card.Root>
