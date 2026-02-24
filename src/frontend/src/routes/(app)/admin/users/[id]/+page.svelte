<script lang="ts">
	import { resolve } from '$app/paths';
	import { buttonVariants } from '$lib/components/ui/button';
	import { UserDetailCards, AuditTrailCard } from '$lib/components/admin';
	import { ArrowLeft, EyeOff } from '@lucide/svelte';
	import { hasPermission, Permissions, cn } from '$lib/utils';
	import * as m from '$lib/paraglide/messages';
	import type { PageData } from './$types';

	let { data }: { data: PageData } = $props();

	let piiMasked = $derived(!hasPermission(data.user, Permissions.Users.ViewPii));
</script>

<!-- eslint-disable svelte/no-navigation-without-resolve -- hrefs are pre-resolved using resolve() -->
<svelte:head>
	<title>{m.meta_titleTemplate({ title: m.meta_adminUserDetail_title() })}</title>
	<meta name="description" content={m.meta_adminUserDetail_description()} />
</svelte:head>

<div class="space-y-6">
	<div class="flex items-center gap-4">
		<a
			href={resolve('/admin/users')}
			class={cn(buttonVariants({ variant: 'ghost', size: 'icon' }), 'h-10 w-10')}
			aria-label={m.admin_userDetail_backToUsers()}
		>
			<ArrowLeft class="h-4 w-4" />
		</a>
		<div>
			<h3 class="text-lg font-medium">
				{#if data.adminUser?.firstName || data.adminUser?.lastName}
					{[data.adminUser?.firstName, data.adminUser?.lastName].filter(Boolean).join(' ')}
				{:else}
					{data.adminUser?.username}
				{/if}
			</h3>
			{#if piiMasked}
				<p class="inline-flex items-center gap-1.5 text-sm text-muted-foreground italic">
					<EyeOff class="h-3.5 w-3.5" aria-hidden="true" />
					{m.admin_pii_emailMasked()}
				</p>
			{:else}
				<p class="text-sm text-muted-foreground">{data.adminUser?.email}</p>
			{/if}
		</div>
	</div>
	<div class="h-px w-full bg-border"></div>

	{#if data.adminUser && data.user}
		<UserDetailCards
			user={data.adminUser}
			roles={data.roles ?? []}
			currentUser={data.user}
			rolesLoadFailed={data.rolesLoadFailed}
		/>
	{/if}

	{#if data.adminUser}
		<AuditTrailCard userId={data.adminUser.id ?? ''} />
	{/if}
</div>
