<script lang="ts">
	import { Construction } from '@lucide/svelte';
	import * as Card from '$lib/components/ui/card';
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
	<Card.Root
		class="relative flex min-h-96 flex-col items-center justify-center overflow-hidden p-16 text-center"
	>
		<!-- Ambient glow -->
		<div class="glow-xl-top-end animate-pulse-slow opacity-40"></div>
		<div class="glow-xl-bottom-start animate-pulse-slow animation-delay-2000 opacity-40"></div>

		<!-- Pulsing icon container -->
		<div class="relative mb-6">
			<div
				class="flex h-20 w-20 items-center justify-center rounded-full border border-warning/30 bg-gradient-to-br from-warning/10 to-amber-500/10"
			>
				<Construction class="h-10 w-10 text-warning" />
			</div>
			<!-- Ping effect -->
			<div
				class="absolute inset-0 animate-ping rounded-full border border-warning/20 opacity-75"
				style="animation-duration: 2s;"
			></div>
		</div>

		<h2 class="relative text-2xl font-bold tracking-tight">{wipTitle ?? m.wip_title()}</h2>
		<p class="relative mt-4 max-w-lg text-muted-foreground">
			{wipDescription ?? m.wip_description()}
		</p>
	</Card.Root>
</div>
