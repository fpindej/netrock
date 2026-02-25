<script lang="ts">
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import { SvelteURLSearchParams } from 'svelte/reactivity';
	import * as Card from '$lib/components/ui/card';
	import { Input } from '$lib/components/ui/input';
	import { Button } from '$lib/components/ui/button';
	import { UserTable, Pagination, CreateUserDialog } from '$lib/components/admin';
	import { Search, UserPlus } from '@lucide/svelte';
	import { hasPermission, Permissions } from '$lib/utils';
	import * as m from '$lib/paraglide/messages';
	import type { PageData } from './$types';

	let { data }: { data: PageData } = $props();

	let searchInput = $state(data.search ?? '');
	let searchTimeout: ReturnType<typeof setTimeout>;
	let inviteDialogOpen = $state(false);

	let canManageUsers = $derived(hasPermission(data.user, Permissions.Users.Manage));
	let piiMasked = $derived(!hasPermission(data.user, Permissions.Users.ViewPii));

	// page.url.pathname is already resolved — no need to pass through resolve()
	function handleSearch(value: string) {
		clearTimeout(searchTimeout);
		searchTimeout = setTimeout(() => {
			const params = new SvelteURLSearchParams(page.url.searchParams);
			if (value) {
				params.set('search', value);
			} else {
				params.delete('search');
			}
			params.delete('page');
			const query = params.toString();
			// eslint-disable-next-line svelte/no-navigation-without-resolve -- page.url.pathname is already resolved
			goto(`${page.url.pathname}${query ? `?${query}` : ''}`, {
				replaceState: true,
				keepFocus: true
			});
		}, 300);
	}

	function handlePageChange(newPage: number) {
		const params = new SvelteURLSearchParams(page.url.searchParams);
		params.set('page', String(newPage));
		// eslint-disable-next-line svelte/no-navigation-without-resolve -- page.url.pathname is already resolved
		goto(`${page.url.pathname}?${params.toString()}`, { replaceState: true });
	}
</script>

<svelte:head>
	<title>{m.meta_titleTemplate({ title: m.meta_adminUsers_title() })}</title>
	<meta name="description" content={m.meta_adminUsers_description()} />
</svelte:head>

<div class="space-y-6">
	<div>
		<h3 class="text-lg font-medium">{m.admin_users_title()}</h3>
		<p class="text-sm text-muted-foreground">{m.admin_users_description()}</p>
	</div>
	<div class="h-px w-full bg-border"></div>

	<div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
		<div class="relative max-w-sm flex-1">
			<Search class="absolute start-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
			<Input
				type="search"
				placeholder={m.admin_users_searchPlaceholder()}
				class="ps-9"
				value={searchInput}
				oninput={(e) => {
					searchInput = e.currentTarget.value;
					handleSearch(searchInput);
				}}
			/>
		</div>
		<div class="flex items-center justify-between gap-3 sm:justify-end">
			{#if data.users?.totalCount != null}
				<p class="text-sm text-muted-foreground">
					{m.admin_users_totalUsers({ count: data.users.totalCount })}
				</p>
			{/if}
			{#if canManageUsers}
				<Button size="default" onclick={() => (inviteDialogOpen = true)}>
					<UserPlus class="me-2 h-4 w-4" />
					{m.admin_users_inviteUser()}
				</Button>
			{/if}
		</div>
	</div>

	<Card.Root>
		<Card.Content class="p-0">
			<UserTable users={data.users?.items ?? []} {piiMasked} />
		</Card.Content>
	</Card.Root>

	<Pagination
		pageNumber={data.users?.pageNumber ?? 1}
		totalPages={data.users?.totalPages ?? 1}
		hasPreviousPage={data.users?.hasPreviousPage ?? false}
		hasNextPage={data.users?.hasNextPage ?? false}
		onPageChange={handlePageChange}
	/>
</div>

{#if canManageUsers}
	<CreateUserDialog bind:open={inviteDialogOpen} />
{/if}
