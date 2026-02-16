<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import { Badge } from '$lib/components/ui/badge';
	import { Shield, Users } from '@lucide/svelte';
	import { resolve } from '$app/paths';
	import type { AdminRole } from '$lib/types';
	import * as m from '$lib/paraglide/messages';

	interface Props {
		roles: AdminRole[];
	}

	let { roles }: Props = $props();

	interface PermissionsByCategory {
		category: string;
		actions: string[];
	}

	function groupPermissions(permissions: string[]): PermissionsByCategory[] {
		const groups: Record<string, string[]> = {};
		for (const perm of permissions) {
			const dotIdx = perm.indexOf('.');
			const category = dotIdx > 0 ? perm.slice(0, dotIdx) : perm;
			const action = dotIdx > 0 ? perm.slice(dotIdx + 1) : perm;
			(groups[category] ??= []).push(action);
		}
		return Object.entries(groups).map(([category, actions]) => ({ category, actions }));
	}
</script>

{#if roles.length === 0}
	<div class="flex flex-col items-center justify-center py-12 text-center">
		<div class="mb-3 rounded-full bg-muted p-3">
			<Shield class="h-6 w-6 text-muted-foreground" />
		</div>
		<p class="text-sm text-muted-foreground">{m.admin_roles_noResults()}</p>
	</div>
{:else}
	<div class="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
		{#each roles as role (role.id)}
			{@const groups = groupPermissions(role.permissions ?? [])}
			<!-- eslint-disable svelte/no-navigation-without-resolve -- href is pre-resolved -->
			<a href={resolve(`/admin/roles/${role.id}`)} class="group">
				<Card.Root
					class="h-full transition-colors group-hover:border-foreground/20 group-hover:bg-muted/50"
				>
					<Card.Header class="pb-3">
						<div class="flex items-center gap-2">
							<Badge variant="secondary" class="text-sm">{role.name}</Badge>
							{#if role.isSystem}
								<Badge variant="outline" class="text-xs">{m.admin_roles_system()}</Badge>
							{/if}
						</div>
						{#if role.description}
							<p class="text-sm text-muted-foreground">{role.description}</p>
						{/if}
					</Card.Header>
					<Card.Content class="space-y-3 pt-0">
						{#if groups.length > 0}
							<div class="space-y-2">
								{#each groups as group (group.category)}
									<div>
										<span class="text-xs font-medium text-muted-foreground capitalize">
											{group.category}
										</span>
										<div class="mt-1 flex flex-wrap gap-1">
											{#each group.actions as action (action)}
												<Badge variant="outline" class="text-xs font-normal">
													{action.replaceAll('_', ' ')}
												</Badge>
											{/each}
										</div>
									</div>
								{/each}
							</div>
						{:else}
							<p class="text-xs text-muted-foreground">
								{m.admin_roles_noPermissions()}
							</p>
						{/if}
					</Card.Content>
					<Card.Footer class="pt-0">
						<div class="flex items-center gap-1 text-xs text-muted-foreground">
							<Users class="h-3.5 w-3.5" />
							<span>{m.admin_roles_userCountLabel({ count: role.userCount ?? 0 })}</span>
						</div>
					</Card.Footer>
				</Card.Root>
			</a>
		{/each}
	</div>
{/if}
