<script lang="ts">
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { Badge } from '$lib/components/ui/badge';
	import { Users, ChevronRight } from '@lucide/svelte';
	import type { AdminUser } from '$lib/types';
	import * as m from '$lib/paraglide/messages';

	interface Props {
		users: AdminUser[];
	}

	let { users }: Props = $props();

	function displayName(user: AdminUser): string {
		if (user.firstName || user.lastName) {
			return [user.firstName, user.lastName].filter(Boolean).join(' ');
		}
		return user.username ?? '';
	}

	function navigateToUser(userId: string | undefined): void {
		if (!userId) return;
		goto(resolve(`/admin/users/${userId}`));
	}
</script>

<!-- eslint-disable svelte/no-navigation-without-resolve -- hrefs are pre-resolved using resolve() -->
{#if users.length === 0}
	<div class="flex flex-col items-center justify-center py-12 text-center">
		<div class="mb-3 rounded-full bg-muted p-3">
			<Users class="h-6 w-6 text-muted-foreground" />
		</div>
		<p class="text-sm text-muted-foreground">{m.admin_users_noResults()}</p>
	</div>
{:else}
	<!-- Mobile: card list -->
	<div class="divide-y md:hidden">
		{#each users as user (user.id)}
			<a
				href={resolve(`/admin/users/${user.id}`)}
				class="flex items-center gap-3 p-4 transition-colors hover:bg-muted/50"
			>
				<div class="min-w-0 flex-1">
					<div class="flex items-center gap-2">
						<p class="truncate text-sm font-medium">
							{displayName(user)}
						</p>
						{#if user.isLockedOut}
							<Badge variant="destructive" class="shrink-0 text-xs">{m.admin_users_locked()}</Badge>
						{/if}
					</div>
					<p class="mt-0.5 truncate text-xs text-muted-foreground">{user.email}</p>
					{#if (user.roles ?? []).length > 0}
						<div class="mt-1.5 flex flex-wrap gap-1">
							{#each user.roles ?? [] as role (role)}
								<Badge variant="secondary" class="text-xs">{role}</Badge>
							{/each}
						</div>
					{/if}
				</div>
				<ChevronRight class="h-4 w-4 shrink-0 text-muted-foreground" />
			</a>
		{/each}
	</div>

	<!-- Desktop: table -->
	<div class="hidden overflow-x-auto md:block">
		<table class="w-full text-sm">
			<thead>
				<tr class="border-b bg-muted/50 text-start">
					<th class="px-4 py-3 text-start text-xs font-medium tracking-wide text-muted-foreground">
						{m.admin_users_name()}
					</th>
					<th class="px-4 py-3 text-start text-xs font-medium tracking-wide text-muted-foreground">
						{m.admin_users_email()}
					</th>
					<th class="px-4 py-3 text-start text-xs font-medium tracking-wide text-muted-foreground">
						{m.admin_users_roles()}
					</th>
					<th
						class="hidden px-4 py-3 text-start text-xs font-medium tracking-wide text-muted-foreground lg:table-cell"
					>
						{m.admin_users_status()}
					</th>
					<th class="w-10 px-4 py-3">
						<span class="sr-only">{m.admin_users_viewDetails()}</span>
					</th>
				</tr>
			</thead>
			<tbody>
				{#each users as user (user.id)}
					<tr
						class="cursor-pointer border-b transition-colors hover:bg-muted/50"
						onclick={() => navigateToUser(user.id)}
						role="link"
						aria-label={m.admin_users_viewDetails()}
						tabindex="0"
						onkeydown={(e: KeyboardEvent) => {
							if (e.key === 'Enter' || e.key === ' ') {
								e.preventDefault();
								navigateToUser(user.id);
							}
						}}
					>
						<td class="px-4 py-3 font-medium">
							<span class="truncate">{displayName(user)}</span>
						</td>
						<td class="px-4 py-3 text-muted-foreground">
							<span class="truncate">{user.email}</span>
						</td>
						<td class="px-4 py-3">
							<div class="flex flex-wrap gap-1">
								{#each user.roles ?? [] as role (role)}
									<Badge variant="secondary" class="text-xs">{role}</Badge>
								{:else}
									<span class="text-xs text-muted-foreground">&mdash;</span>
								{/each}
							</div>
						</td>
						<td class="hidden px-4 py-3 lg:table-cell">
							{#if user.isLockedOut}
								<Badge variant="destructive" class="text-xs">{m.admin_users_locked()}</Badge>
							{:else}
								<Badge variant="outline" class="text-xs">{m.admin_users_active()}</Badge>
							{/if}
						</td>
						<td class="px-4 py-3 text-end">
							<ChevronRight class="h-4 w-4 text-muted-foreground" />
						</td>
					</tr>
				{/each}
			</tbody>
		</table>
	</div>
{/if}
