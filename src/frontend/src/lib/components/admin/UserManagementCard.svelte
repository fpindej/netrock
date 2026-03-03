<script lang="ts">
	import { ReadOnlyNotice } from '$lib/components/common';
	import * as Card from '$lib/components/ui/card';
	import * as Alert from '$lib/components/ui/alert';
	import { RoleManagement, AccountActions } from '$lib/components/admin';
	import { TriangleAlert } from '@lucide/svelte';
	import type { AdminUser, AdminRole } from '$lib/types';
	import type { Cooldown } from '$lib/state';
	import * as m from '$lib/paraglide/messages';

	interface Props {
		user: AdminUser;
		roles: AdminRole[];
		canManage: boolean;
		canAssignRoles: boolean;
		callerRank: number;
		cooldown: Cooldown;
		rolesLoadFailed?: boolean;
	}

	let {
		user,
		roles,
		canManage,
		canAssignRoles,
		callerRank,
		cooldown,
		rolesLoadFailed = false
	}: Props = $props();
</script>

<Card.Root>
	<Card.Header>
		<Card.Title>{m.admin_userDetail_roleManagement()}</Card.Title>
		<Card.Description>{m.admin_userDetail_roleManagementDescription()}</Card.Description>
	</Card.Header>
	<Card.Content class="space-y-4">
		{#if rolesLoadFailed}
			<Alert.Root>
				<TriangleAlert class="h-4 w-4" />
				<Alert.Description>{m.admin_warning_rolesUnavailable()}</Alert.Description>
			</Alert.Root>
		{/if}
		{#if !canManage && !canAssignRoles}
			<ReadOnlyNotice message={m.admin_userDetail_cannotManage()} />
		{/if}

		<div class={!canManage && !canAssignRoles ? 'opacity-60' : ''}>
			<RoleManagement {user} {roles} {canAssignRoles} {callerRank} {cooldown} />
			<AccountActions {user} {canManage} {cooldown} />
		</div>
	</Card.Content>
</Card.Root>
