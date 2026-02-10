<script lang="ts">
	import { AccountInfoCard, UserManagementCard } from '$lib/components/admin';
	import type { AdminUser, AdminRole, User } from '$lib/types';

	interface Props {
		user: AdminUser;
		roles: AdminRole[];
		currentUser: User;
	}

	let { user, roles, currentUser }: Props = $props();
</script>

<div class="grid gap-6 lg:grid-cols-2">
	<AccountInfoCard {user} />
	<UserManagementCard {user} {roles} {currentUser} />
</div>
