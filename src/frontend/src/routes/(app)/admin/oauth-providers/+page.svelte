<script lang="ts">
	import { OAuthProviderCard } from '$lib/components/admin';
	import { hasPermission, Permissions } from '$lib/utils';
	import * as m from '$lib/paraglide/messages';
	import type { PageData } from './$types';

	let { data }: { data: PageData } = $props();

	let canManage = $derived(hasPermission(data.user, Permissions.OAuthProviders.Manage));
</script>

<svelte:head>
	<title>{m.meta_titleTemplate({ title: m.meta_adminOAuthProviders_title() })}</title>
	<meta name="description" content={m.meta_adminOAuthProviders_description()} />
</svelte:head>

<div class="space-y-6">
	<div>
		<h3 class="text-lg font-medium">{m.admin_oauthProviders_title()}</h3>
		<p class="text-sm text-muted-foreground">{m.admin_oauthProviders_description()}</p>
	</div>
	<div class="h-px w-full bg-border"></div>

	{#if data.providers.length === 0}
		<p class="text-sm text-muted-foreground">{m.admin_oauthProviders_noProviders()}</p>
	{:else}
		<div class="grid gap-6 lg:grid-cols-2">
			{#each data.providers as provider (provider.provider)}
				<OAuthProviderCard {provider} {canManage} />
			{/each}
		</div>
	{/if}
</div>
