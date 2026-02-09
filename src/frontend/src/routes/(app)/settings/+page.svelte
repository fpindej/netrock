<script lang="ts">
	import { ChangePasswordForm } from '$lib/components/settings';
	import * as m from '$lib/paraglide/messages';
</script>

<svelte:head>
	<title>{m.meta_titleTemplate({ title: m.meta_settings_title() })}</title>
	<meta name="description" content={m.meta_settings_description()} />
</svelte:head>

<div class="space-y-6">
	<div>
		<h3 class="text-lg font-medium">{m.settings_title()}</h3>
		<p class="text-sm text-muted-foreground">{m.settings_description()}</p>
	</div>
	<div class="h-px w-full bg-border"></div>
	<div class="max-w-2xl">
		<ChangePasswordForm />
	</div>
</div>
