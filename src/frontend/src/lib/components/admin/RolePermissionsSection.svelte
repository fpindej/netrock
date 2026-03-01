<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import * as Alert from '$lib/components/ui/alert';
	import { Button } from '$lib/components/ui/button';
	import { RolePermissionEditor } from '$lib/components/admin';
	import { Lock, Loader2, Save, TriangleAlert } from '@lucide/svelte';
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
		permissionsLoadFailed?: boolean;
	}

	let {
		roleId,
		permissionGroups,
		selectedPermissions = $bindable(),
		canEditPermissions,
		cooldown,
		permissionsLoadFailed = false
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
		{#if permissionsLoadFailed}
			<Alert.Root>
				<TriangleAlert class="h-4 w-4" />
				<Alert.Description>{m.admin_warning_permissionsUnavailable()}</Alert.Description>
			</Alert.Root>
		{/if}
		{#if !canEditPermissions}
			<div
				class="flex items-center gap-2 rounded-md bg-muted px-3 py-2 text-sm text-muted-foreground"
			>
				<Lock class="h-4 w-4 shrink-0" />
				<span>{m.common_readOnlyNotice()}</span>
			</div>
		{/if}
		<RolePermissionEditor
			{permissionGroups}
			selected={selectedPermissions}
			disabled={!canEditPermissions}
			onchange={(perms) => (selectedPermissions = perms)}
		/>
		{#if canEditPermissions}
			<div class="flex flex-col gap-2 sm:flex-row sm:justify-end">
				<Button
					class="w-full sm:w-auto"
					disabled={isSavingPermissions || cooldown.active}
					onclick={savePermissions}
				>
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
			</div>
		{/if}
	</Card.Content>
</Card.Root>
