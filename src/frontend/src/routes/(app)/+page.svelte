<script lang="ts">
	import { PageHeader } from '$lib/components/common';
	import * as m from '$lib/paraglide/messages';
</script>

<svelte:head>
	<title>{m.meta_titleTemplate({ title: m.meta_dashboard_title() })}</title>
	<meta name="description" content={m.meta_dashboard_description()} />
</svelte:head>

<div class="space-y-6">
	<PageHeader title={m.meta_dashboard_title()} description={m.meta_dashboard_description()} />
</div>
