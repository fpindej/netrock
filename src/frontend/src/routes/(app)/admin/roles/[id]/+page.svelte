<script lang="ts">
	import { Badge } from '$lib/components/ui/badge';
	import { Separator } from '$lib/components/ui/separator';
	import {
		RoleDetailsCard,
		RolePermissionsSection,
		RoleDeleteSection
	} from '$lib/components/admin';
	import { AdminBreadcrumb } from '$lib/components/common';
	import { resolve } from '$app/paths';
	import { createCooldown } from '$lib/state';
	import { hasPermission, Permissions, SystemRoles } from '$lib/utils';
	import * as m from '$lib/paraglide/messages';
	import type { PageData } from './$types';

	let { data }: { data: PageData } = $props();

	let canManageRoles = $derived(hasPermission(data.user, Permissions.Roles.Manage));
	let isSuperAdmin = $derived(data.role?.name === SystemRoles.SuperAdmin);
	let isSystem = $derived(data.role?.isSystem ?? false);
	let canEditPermissions = $derived(canManageRoles && !isSuperAdmin);
	let canEditName = $derived(canManageRoles && !isSystem);
	let canDelete = $derived(canManageRoles && !isSystem && (data.role?.userCount ?? 0) === 0);

	let roleName = $state(data.role?.name ?? '');
	let roleDescription = $state(data.role?.description ?? '');
	let selectedPermissions = $state<string[]>(data.role?.permissions ?? []);

	const cooldown = createCooldown();
</script>

<svelte:head>
	<title>{m.meta_titleTemplate({ title: data.role?.name ?? m.meta_adminRoles_title() })}</title>
</svelte:head>

<div class="space-y-6">
	<div class="space-y-1">
		<AdminBreadcrumb
			items={[
				{ label: m.nav_adminRoles(), href: resolve('/admin/roles') },
				{ label: data.role?.name ?? '' }
			]}
		/>
		<div class="flex items-center gap-3">
			{#if isSystem}
				<Badge variant="outline">{m.admin_roles_system()}</Badge>
			{/if}
			<span class="text-sm text-muted-foreground">
				{m.admin_roles_userCountLabel({ count: data.role?.userCount ?? 0 })}
			</span>
		</div>
	</div>
	<Separator />

	<RoleDetailsCard
		roleId={data.role?.id ?? ''}
		bind:name={roleName}
		bind:description={roleDescription}
		{isSystem}
		{canEditName}
		{canManageRoles}
		{cooldown}
	/>

	<RolePermissionsSection
		roleId={data.role?.id ?? ''}
		permissionGroups={data.permissionGroups}
		bind:selectedPermissions
		{canEditPermissions}
		{cooldown}
		permissionsLoadFailed={data.permissionsLoadFailed}
	/>

	{#if canDelete}
		<RoleDeleteSection roleId={data.role?.id ?? ''} {cooldown} />
	{/if}
</div>
