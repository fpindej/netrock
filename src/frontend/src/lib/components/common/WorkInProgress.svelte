<script lang="ts">
	import { Construction } from '@lucide/svelte';
	import * as Alert from '$lib/components/ui/alert';
	import * as m from '$lib/paraglide/messages';

	interface Props {
		title: string;
		description?: string;
		wipTitle?: string;
		wipDescription?: string;
	}

	let { title, description, wipTitle, wipDescription }: Props = $props();
</script>

<div class="space-y-8">
	<div>
		<h3 class="text-lg font-medium">{title}</h3>
		{#if description}
			<p class="text-sm text-muted-foreground">{description}</p>
		{/if}
	</div>
	<Alert.Root>
		<Construction class="h-5 w-5" />
		<Alert.Title>{wipTitle ?? m.wip_title()}</Alert.Title>
		<Alert.Description>{wipDescription ?? m.wip_description()}</Alert.Description>
	</Alert.Root>
</div>
