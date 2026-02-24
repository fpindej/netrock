<script lang="ts">
	import { Badge } from '$lib/components/ui/badge';
	import { Button } from '$lib/components/ui/button';
	import * as Select from '$lib/components/ui/select';
	import { browserClient, handleMutationError } from '$lib/api';
	import { toast } from '$lib/components/ui/sonner';
	import { invalidateAll } from '$app/navigation';
	import { Plus, X, Loader2 } from '@lucide/svelte';
	import type { AdminUser, AdminRole } from '$lib/types';
	import { getRoleRank } from '$lib/utils';
	import type { Cooldown } from '$lib/state';
	import * as m from '$lib/paraglide/messages';

	interface Props {
		user: AdminUser;
		roles: AdminRole[];
		canAssignRoles: boolean;
		callerRank: number;
		cooldown: Cooldown;
	}

	let { user, roles, canAssignRoles, callerRank, cooldown }: Props = $props();

	let isAssigningRole = $state(false);
	let isRemovingRole = $state<string | null>(null);
	let selectedRole = $state('');

	let targetRoles = $derived(user.roles ?? []);

	let assignableRoles = $derived(
		(roles ?? [])
			.map((r) => r.name ?? '')
			.filter((role) => getRoleRank(role) < callerRank && !targetRoles.includes(role))
	);

	function canRemoveRole(role: string): boolean {
		return canAssignRoles && getRoleRank(role) < callerRank;
	}

	async function assignRole() {
		if (!selectedRole) return;
		isAssigningRole = true;
		const { response, error } = await browserClient.POST('/api/v1/admin/users/{id}/roles', {
			params: { path: { id: user.id ?? '' } },
			body: { role: selectedRole }
		});
		isAssigningRole = false;

		if (response.ok) {
			toast.success(m.admin_userDetail_roleAssigned());
			selectedRole = '';
			await invalidateAll();
		} else {
			handleMutationError(response, error, {
				cooldown,
				fallback: m.admin_userDetail_roleAssignError()
			});
		}
	}

	async function removeRole(role: string) {
		isRemovingRole = role;
		const { response, error } = await browserClient.DELETE(
			'/api/v1/admin/users/{id}/roles/{role}',
			{
				params: { path: { id: user.id ?? '', role } }
			}
		);
		isRemovingRole = null;

		if (response.ok) {
			toast.success(m.admin_userDetail_roleRemoved());
			await invalidateAll();
		} else {
			handleMutationError(response, error, {
				cooldown,
				fallback: m.admin_userDetail_roleRemoveError()
			});
		}
	}
</script>

<!-- Current roles -->
<div>
	<p class="mb-2 text-sm font-medium">{m.admin_userDetail_currentRoles()}</p>
	<div class="flex flex-wrap gap-2">
		{#each user.roles ?? [] as role (role)}
			<Badge variant="secondary" class="gap-1 py-1 text-sm">
				{role}
				{#if canRemoveRole(role)}
					<button
						class="ms-1 inline-flex h-5 w-5 items-center justify-center rounded-full transition-colors hover:bg-muted-foreground/20"
						aria-label="{m.admin_userDetail_removeRole()} {role}"
						disabled={isRemovingRole === role}
						onclick={() => removeRole(role)}
					>
						{#if isRemovingRole === role}
							<Loader2 class="h-3 w-3 animate-spin" />
						{:else}
							<X class="h-3 w-3" />
						{/if}
					</button>
				{/if}
			</Badge>
		{:else}
			<span class="text-sm text-muted-foreground">{m.admin_userDetail_noRoles()}</span>
		{/each}
	</div>
</div>

<!-- Assign role -->
{#if canAssignRoles && assignableRoles.length > 0}
	<div class="mt-4 flex flex-col gap-2 sm:flex-row sm:items-end">
		<div class="flex-1">
			<label for="role-select" class="mb-1 block text-sm font-medium">
				{m.admin_userDetail_assignRole()}
			</label>
			<Select.Root type="single" bind:value={selectedRole}>
				<Select.Trigger id="role-select">
					{#if selectedRole}
						<span>{selectedRole}</span>
					{:else}
						<span class="text-muted-foreground">{m.admin_userDetail_selectRole()}</span>
					{/if}
				</Select.Trigger>
				<Select.Content>
					{#each assignableRoles as role (role)}
						<Select.Item value={role}>{role}</Select.Item>
					{/each}
				</Select.Content>
			</Select.Root>
		</div>
		<Button
			size="default"
			class="shrink-0"
			disabled={!selectedRole || isAssigningRole || cooldown.active}
			onclick={assignRole}
		>
			{#if cooldown.active}
				{m.common_waitSeconds({ seconds: cooldown.remaining })}
			{:else if isAssigningRole}
				<Loader2 class="me-1 h-4 w-4 animate-spin" />
				{m.admin_userDetail_assignRole()}
			{:else}
				<Plus class="me-1 h-4 w-4" />
				{m.admin_userDetail_assignRole()}
			{/if}
		</Button>
	</div>
{/if}
