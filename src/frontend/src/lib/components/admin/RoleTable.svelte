<script lang="ts">
	import { Badge } from '$lib/components/ui/badge';
	import { Shield } from '@lucide/svelte';
	import { resolve } from '$app/paths';
	import type { AdminRole } from '$lib/types';
	import * as m from '$lib/paraglide/messages';

	interface Props {
		roles: AdminRole[];
	}

	let { roles }: Props = $props();
</script>

{#if roles.length === 0}
	<div class="flex flex-col items-center justify-center py-12 text-center">
		<div class="mb-3 rounded-full bg-muted p-3">
			<Shield class="h-6 w-6 text-muted-foreground" />
		</div>
		<p class="text-sm text-muted-foreground">{m.admin_roles_noResults()}</p>
	</div>
{:else}
	<!-- Mobile: card list -->
	<div class="divide-y md:hidden">
		{#each roles as role (role.id)}
			<!-- eslint-disable svelte/no-navigation-without-resolve -- href is pre-resolved -->
			<a
				href={resolve(`/admin/roles/${role.id}`)}
				class="flex items-center justify-between p-4 transition-colors hover:bg-muted/50"
			>
				<div class="flex items-center gap-2">
					<Badge variant="secondary" class="text-sm">{role.name}</Badge>
					{#if role.isSystem}
						<Badge variant="outline" class="text-xs">{m.admin_roles_system()}</Badge>
					{/if}
				</div>
				<span class="text-sm text-muted-foreground tabular-nums">
					{role.userCount ?? 0}
					{m.admin_roles_userCount().toLowerCase()}
				</span>
			</a>
		{/each}
	</div>

	<!-- Desktop: table -->
	<div class="hidden overflow-x-auto md:block">
		<table class="w-full text-sm">
			<thead>
				<tr class="border-b bg-muted/50 text-start">
					<th class="px-4 py-3 text-start text-xs font-medium tracking-wide text-muted-foreground">
						{m.admin_roles_name()}
					</th>
					<th class="px-4 py-3 text-start text-xs font-medium tracking-wide text-muted-foreground">
						{m.admin_roles_description()}
					</th>
					<th class="px-4 py-3 text-end text-xs font-medium tracking-wide text-muted-foreground">
						{m.admin_roles_userCount()}
					</th>
				</tr>
			</thead>
			<tbody>
				{#each roles as role (role.id)}
					<!-- eslint-disable svelte/no-navigation-without-resolve -- href is pre-resolved -->
					<tr class="border-b transition-colors hover:bg-muted/50">
						<td class="px-4 py-3">
							<a href={resolve(`/admin/roles/${role.id}`)} class="flex items-center gap-2">
								<Badge variant="secondary">{role.name}</Badge>
								{#if role.isSystem}
									<Badge variant="outline" class="text-xs">{m.admin_roles_system()}</Badge>
								{/if}
							</a>
						</td>
						<td class="px-4 py-3 text-muted-foreground">
							{role.description ?? ''}
						</td>
						<td class="px-4 py-3 text-end tabular-nums">{role.userCount ?? 0}</td>
					</tr>
				{/each}
			</tbody>
		</table>
	</div>
{/if}
