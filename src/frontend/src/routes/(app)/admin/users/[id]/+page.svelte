<script lang="ts">
	import { Separator } from '$lib/components/ui/separator';
	import { UserDetailCards, AuditTrailCard } from '$lib/components/admin';
	import { EyeOff } from '@lucide/svelte';
	import { hasPermission, Permissions } from '$lib/utils';
	import { setDynamicLabel, clearDynamicLabel } from '$lib/state/breadcrumb.svelte';
	import * as m from '$lib/paraglide/messages';
	import type { PageData } from './$types';

	let { data }: { data: PageData } = $props();

	let piiMasked = $derived(!hasPermission(data.user, Permissions.Users.ViewPii));

	let displayName = $derived(
		data.adminUser?.firstName || data.adminUser?.lastName
			? [data.adminUser?.firstName, data.adminUser?.lastName].filter(Boolean).join(' ')
			: (data.adminUser?.username ?? '')
	);

	$effect(() => {
		setDynamicLabel(displayName);
		return () => clearDynamicLabel();
	});
</script>

<svelte:head>
	<title>{m.meta_titleTemplate({ title: m.meta_adminUserDetail_title() })}</title>
	<meta name="description" content={m.meta_adminUserDetail_description()} />
</svelte:head>

<div class="space-y-6">
	<div class="space-y-1">
		{#if piiMasked}
			<p class="inline-flex items-center gap-1.5 text-sm text-muted-foreground italic">
				<EyeOff class="h-3.5 w-3.5" aria-hidden="true" />
				{m.admin_pii_emailMasked()}
			</p>
		{:else}
			<p class="text-sm text-muted-foreground">{data.adminUser?.email}</p>
		{/if}
	</div>
	<Separator />

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
