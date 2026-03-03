<script lang="ts">
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { EmptyState } from '$lib/components/common';
	import { Badge } from '$lib/components/ui/badge';
	import * as Table from '$lib/components/ui/table';
	import * as Tooltip from '$lib/components/ui/tooltip';
	import { Users, ChevronRight, EyeOff } from '@lucide/svelte';
	import type { AdminUser } from '$lib/types';
	import * as m from '$lib/paraglide/messages';

	interface Props {
		users: AdminUser[];
		piiMasked?: boolean;
	}

	let { users, piiMasked = false }: Props = $props();

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
	<EmptyState icon={Users} message={m.admin_users_noResults()} />
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
					<p class="mt-0.5 truncate text-xs text-muted-foreground" class:italic={piiMasked}>
						{user.email}
					</p>
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
	<div class="hidden md:block">
		<Table.Root>
			<Table.Header>
				<Table.Row class="bg-muted/50">
					<Table.Head class="px-4 py-3 text-xs tracking-wide">
						{m.admin_users_name()}
					</Table.Head>
					<Table.Head class="px-4 py-3 text-xs tracking-wide">
						<span class="inline-flex items-center gap-1.5">
							{m.admin_users_email()}
							{#if piiMasked}
								<Tooltip.Root>
									<Tooltip.Trigger aria-label={m.admin_pii_maskedTooltip()}>
										<EyeOff class="h-3.5 w-3.5 text-muted-foreground/60" aria-hidden="true" />
									</Tooltip.Trigger>
									<Tooltip.Portal>
										<Tooltip.Content>
											{m.admin_pii_maskedTooltip()}
										</Tooltip.Content>
									</Tooltip.Portal>
								</Tooltip.Root>
							{/if}
						</span>
					</Table.Head>
					<Table.Head class="px-4 py-3 text-xs tracking-wide">
						{m.admin_users_roles()}
					</Table.Head>
					<Table.Head class="hidden px-4 py-3 text-xs tracking-wide lg:table-cell">
						{m.admin_users_status()}
					</Table.Head>
					<Table.Head class="w-10 px-4 py-3">
						<span class="sr-only">{m.admin_users_viewDetails()}</span>
					</Table.Head>
				</Table.Row>
			</Table.Header>
			<Table.Body>
				{#each users as user (user.id)}
					<Table.Row
						class="cursor-pointer"
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
						<Table.Cell class="max-w-48 px-4 py-3 font-medium">
							<span class="block truncate">{displayName(user)}</span>
						</Table.Cell>
						<Table.Cell class="max-w-56 px-4 py-3 text-muted-foreground">
							<span class="block truncate" class:italic={piiMasked}>{user.email}</span>
						</Table.Cell>
						<Table.Cell class="px-4 py-3">
							<div class="flex flex-wrap gap-1">
								{#each user.roles ?? [] as role (role)}
									<Badge variant="secondary" class="text-xs">{role}</Badge>
								{:else}
									<span class="text-xs text-muted-foreground">&mdash;</span>
								{/each}
							</div>
						</Table.Cell>
						<Table.Cell class="hidden px-4 py-3 lg:table-cell">
							{#if user.isLockedOut}
								<Badge variant="destructive" class="text-xs">{m.admin_users_locked()}</Badge>
							{:else}
								<Badge variant="outline" class="text-xs">{m.admin_users_active()}</Badge>
							{/if}
						</Table.Cell>
						<Table.Cell class="px-4 py-3 text-end">
							<ChevronRight class="h-4 w-4 text-muted-foreground" />
						</Table.Cell>
					</Table.Row>
				{/each}
			</Table.Body>
		</Table.Root>
	</div>
{/if}
