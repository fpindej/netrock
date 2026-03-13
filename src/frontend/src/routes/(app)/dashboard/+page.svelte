<script lang="ts">
	import { PageHeader } from '$lib/components/common';
	import { WelcomeGuide, QuickActions, AccountStatus } from '$lib/components/dashboard';
	import * as m from '$lib/paraglide/messages';
	import type { PageData } from './$types';

	let { data }: { data: PageData } = $props();

	let user = $derived(data.user);
	let greeting = $derived(
		user?.firstName ? m.dashboard_welcome({ name: user.firstName }) : m.dashboard_welcomeGeneric()
	);
</script>

<svelte:head>
	<title>{m.meta_titleTemplate({ title: m.meta_dashboard_title() })}</title>
	<meta name="description" content={m.meta_dashboard_description()} />
</svelte:head>

<div class="space-y-6">
	<PageHeader title={greeting} description={m.dashboard_subtitle()} />

	<div class="space-y-8">
		<!-- Explainer - remove this component when customizing your dashboard -->
		<WelcomeGuide {user} />

		<!-- Dashboard widgets -->
		<QuickActions {user} />
		<AccountStatus {user} />
	</div>
</div>
