<script lang="ts">
	import { AccountInfoCard, UserManagementCard } from '$lib/components/admin';
	import type { AdminUser, AdminRole, User } from '$lib/types';
	import { canManageUser, getHighestRank, hasPermission, Permissions } from '$lib/utils';
	import { createCooldown } from '$lib/state';

	interface Props {
		user: AdminUser;
		roles: AdminRole[];
		currentUser: User;
	}

	let { user, roles, currentUser }: Props = $props();

	const cooldown = createCooldown();

	let callerRoles = $derived(currentUser.roles ?? []);
	let targetRoles = $derived(user.roles ?? []);
	let canManageByHierarchy = $derived(canManageUser(callerRoles, targetRoles));
	let canManage = $derived(
		canManageByHierarchy && hasPermission(currentUser, Permissions.Users.Manage)
	);
	let canAssignRoles = $derived(
		canManageByHierarchy && hasPermission(currentUser, Permissions.Users.AssignRoles)
	);
	let callerRank = $derived(getHighestRank(callerRoles));
	let piiMasked = $derived(!hasPermission(currentUser, Permissions.Users.ViewPii));
</script>

<div class="grid gap-6 xl:grid-cols-2">
	<AccountInfoCard {user} {canManage} {piiMasked} {cooldown} />
	<UserManagementCard {user} {roles} {canManage} {canAssignRoles} {callerRank} {cooldown} />
</div>
