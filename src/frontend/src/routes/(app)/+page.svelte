<script lang="ts">
	import * as m from '$lib/paraglide/messages';
</script>

<svelte:head>
	<title>{m.meta_titleTemplate({ title: m.meta_dashboard_title() })}</title>
	<meta name="description" content={m.meta_dashboard_description()} />
</svelte:head>

<div class="space-y-6">
	<div>
		<h3 class="text-lg font-medium">{m.meta_dashboard_title()}</h3>
		<p class="text-sm text-muted-foreground">{m.meta_dashboard_description()}</p>
	</div>
	<div class="h-px w-full bg-border"></div>
</div>
