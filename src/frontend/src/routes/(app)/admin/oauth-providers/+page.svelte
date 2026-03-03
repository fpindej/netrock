<script lang="ts">
	import { PageHeader, EmptyState } from '$lib/components/common';
	import { OAuthProviderCard } from '$lib/components/admin';
	import { hasPermission, Permissions } from '$lib/utils';
	import * as m from '$lib/paraglide/messages';
	import { KeyRound } from '@lucide/svelte';
	import type { PageData } from './$types';

	let { data }: { data: PageData } = $props();

	let canManage = $derived(hasPermission(data.user, Permissions.OAuthProviders.Manage));
</script>

<svelte:head>
	<title>{m.meta_titleTemplate({ title: m.meta_adminOAuthProviders_title() })}</title>
	<meta name="description" content={m.meta_adminOAuthProviders_description()} />
</svelte:head>

<div class="space-y-6">
	<PageHeader
		title={m.admin_oauthProviders_title()}
		description={m.admin_oauthProviders_description()}
	/>

	{#if data.providers.length === 0}
		<EmptyState icon={KeyRound} message={m.admin_oauthProviders_noProviders()} />
	{:else}
		<div class="grid gap-6 lg:grid-cols-2">
			{#each data.providers as provider (provider.provider)}
				<OAuthProviderCard {provider} {canManage} />
			{/each}
		</div>
	{/if}
</div>
