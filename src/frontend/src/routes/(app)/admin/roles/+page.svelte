<script lang="ts">
	import { PageHeader } from '$lib/components/common';
	import { Button } from '$lib/components/ui/button';
	import { RoleCardGrid, CreateRoleDialog } from '$lib/components/admin';
	import { hasPermission, Permissions } from '$lib/utils';
	import { Plus } from '@lucide/svelte';
	import * as m from '$lib/paraglide/messages';
	import type { PageData } from './$types';

	let { data }: { data: PageData } = $props();

	let createDialogOpen = $state(false);
	let canManageRoles = $derived(hasPermission(data.user, Permissions.Roles.Manage));
</script>

<svelte:head>
	<title>{m.meta_titleTemplate({ title: m.meta_adminRoles_title() })}</title>
	<meta name="description" content={m.meta_adminRoles_description()} />
</svelte:head>

<div class="space-y-6">
	<PageHeader title={m.admin_roles_title()} description={m.admin_roles_description()}>
		{#snippet actions()}
			{#if canManageRoles}
				<Button onclick={() => (createDialogOpen = true)}>
					<Plus class="me-1 h-4 w-4" />
					{m.admin_roles_createRole()}
				</Button>
			{/if}
		{/snippet}
	</PageHeader>

	<RoleCardGrid roles={data.roles} />
</div>

{#if canManageRoles}
	<CreateRoleDialog bind:open={createDialogOpen} />
{/if}
