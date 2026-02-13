<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import { Button } from '$lib/components/ui/button';
	import { RoleTable, CreateRoleDialog } from '$lib/components/admin';
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
	<div class="flex items-center justify-between">
		<div>
			<h3 class="text-lg font-medium">{m.admin_roles_title()}</h3>
			<p class="text-sm text-muted-foreground">{m.admin_roles_description()}</p>
		</div>
		{#if canManageRoles}
			<Button size="sm" onclick={() => (createDialogOpen = true)}>
				<Plus class="me-1 h-4 w-4" />
				{m.admin_roles_createRole()}
			</Button>
		{/if}
	</div>
	<div class="h-px w-full bg-border"></div>

	<Card.Root>
		<Card.Content class="p-0">
			<RoleTable roles={data.roles} />
		</Card.Content>
	</Card.Root>
</div>

{#if canManageRoles}
	<CreateRoleDialog bind:open={createDialogOpen} />
{/if}
