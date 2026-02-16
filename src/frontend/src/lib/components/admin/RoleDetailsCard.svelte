<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import { Button } from '$lib/components/ui/button';
	import { Input } from '$lib/components/ui/input';
	import { Loader2, Save } from '@lucide/svelte';
	import { browserClient, handleMutationError } from '$lib/api';
	import { toast } from '$lib/components/ui/sonner';
	import { invalidateAll } from '$app/navigation';
	import type { Cooldown } from '$lib/state';
	import * as m from '$lib/paraglide/messages';

	interface Props {
		roleId: string;
		name: string;
		description: string;
		isSystem: boolean;
		canEditName: boolean;
		canManageRoles: boolean;
		cooldown: Cooldown;
	}

	let {
		roleId,
		name = $bindable(),
		description = $bindable(),
		isSystem,
		canEditName,
		canManageRoles,
		cooldown
	}: Props = $props();

	let isSaving = $state(false);

	async function saveRole() {
		isSaving = true;
		const { response, error } = await browserClient.PUT('/api/v1/admin/roles/{id}', {
			params: { path: { id: roleId } },
			body: {
				name: canEditName ? name : null,
				description: description
			}
		});
		isSaving = false;

		if (response.ok) {
			toast.success(m.admin_roles_updateSuccess());
			await invalidateAll();
		} else {
			handleMutationError(response, error, {
				cooldown,
				fallback: m.admin_roles_updateError()
			});
		}
	}
</script>

<Card.Root>
	<Card.Header>
		<Card.Title>{m.admin_roles_detailTitle()}</Card.Title>
		<Card.Description>{m.admin_roles_detailDescription()}</Card.Description>
	</Card.Header>
	<Card.Content class="space-y-4">
		<div>
			<label for="role-name" class="mb-1 block text-sm font-medium">
				{m.admin_roles_name()}
			</label>
			<Input id="role-name" bind:value={name} disabled={!canEditName} maxlength={50} />
			{#if isSystem}
				<p class="mt-1 text-xs text-muted-foreground">{m.admin_roles_systemNameReadonly()}</p>
			{/if}
		</div>
		<div>
			<label for="role-desc" class="mb-1 block text-sm font-medium">
				{m.admin_roles_descriptionLabel()}
			</label>
			<Input
				id="role-desc"
				bind:value={description}
				disabled={!canManageRoles}
				maxlength={200}
				placeholder={m.admin_roles_descriptionPlaceholder()}
			/>
		</div>
		{#if canManageRoles}
			<Button size="sm" disabled={isSaving || cooldown.active} onclick={saveRole}>
				{#if cooldown.active}
					{m.common_waitSeconds({ seconds: cooldown.remaining })}
				{:else if isSaving}
					<Loader2 class="me-2 h-4 w-4 animate-spin" />
					{m.admin_roles_saveDetails()}
				{:else}
					<Save class="me-2 h-4 w-4" />
					{m.admin_roles_saveDetails()}
				{/if}
			</Button>
		{/if}
	</Card.Content>
</Card.Root>
