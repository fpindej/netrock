<script lang="ts">
	import { page } from '$app/state';
	import { invalidateAll } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { Button } from '$lib/components/ui/button';
	import * as Card from '$lib/components/ui/card';
	import {
		Ghost,
		Ban,
		Timer,
		TriangleAlert,
		Home,
		SearchX,
		WifiOff,
		LoaderCircle,
		type IconProps
	} from '@lucide/svelte';
	import * as m from '$lib/paraglide/messages';
	import { healthState } from '$lib/state';
	import type { Component } from 'svelte';

	function getErrorContent(status: number): {
		title: () => string;
		description: () => string;
		icon: Component<IconProps>;
		iconColor: string;
	} {
		switch (status) {
			case 429:
				return {
					title: m.error_429_title,
					description: m.error_429_description,
					icon: Timer,
					iconColor: 'text-warning'
				};
			case 404:
				return {
					title: m.error_404_title,
					description: m.error_404_description,
					icon: SearchX,
					iconColor: 'text-muted-foreground'
				};
			case 403:
				return {
					title: m.error_403_title,
					description: m.error_403_description,
					icon: Ban,
					iconColor: 'text-destructive'
				};
			case 500:
				return {
					title: m.error_500_title,
					description: m.error_500_description,
					icon: TriangleAlert,
					iconColor: 'text-destructive'
				};
			case 503:
				return {
					title: m.error_503_title,
					description: m.error_503_description,
					icon: WifiOff,
					iconColor: 'text-warning'
				};
			default:
				return {
					title: m.error_default_title,
					description: m.error_default_description,
					icon: Ghost,
					iconColor: 'text-warning'
				};
		}
	}

	let status = $derived(page.status);
	let message = $derived(page.error?.message);
	let content = $derived(getErrorContent(status));
	let Icon = $derived(content.icon);
	let is503 = $derived(status === 503);

	// Auto-recover: when health polling detects the backend is back, reload.
	// invalidateAll re-runs server loads — if they succeed, SvelteKit exits the
	// error boundary automatically. The hard reload is only a fallback for when
	// the boundary doesn't clear (e.g. stale client state).
	let recovering = $state(false);
	$effect(() => {
		if (is503 && healthState.online && !recovering) {
			recovering = true;
			invalidateAll().then(() => {
				if (page.status === 503) window.location.reload();
			});
		}
	});
</script>

<div class="flex min-h-screen flex-col justify-center bg-background px-4 py-12 sm:px-6 lg:px-8">
	<div class="sm:mx-auto sm:w-full sm:max-w-md">
		<Card.Root class="text-center shadow-lg">
			<Card.Header>
				<div
					class="mx-auto mb-4 flex h-24 w-24 items-center justify-center rounded-full bg-muted/50 p-4"
				>
					<Icon class="h-12 w-12 {content.iconColor}" />
				</div>
				<Card.Title class="text-4xl font-extrabold tracking-tight">{status}</Card.Title>
				<Card.Description class="mt-2 text-xl font-semibold text-foreground">
					{content.title()}
				</Card.Description>
			</Card.Header>
			<Card.Content>
				<p class="text-muted-foreground">
					{!is503 && message && message !== 'An unexpected error occurred.'
						? message
						: content.description()}
				</p>
			</Card.Content>
			<Card.Footer class="flex justify-center pb-8">
				{#if is503}
					<div class="flex items-center gap-2 text-sm text-muted-foreground">
						<LoaderCircle class="h-4 w-4 animate-spin" />
						{m.error_503_retrying()}
					</div>
				{:else}
					<Button href={resolve('/')} variant="default" size="lg" class="gap-2">
						<Home class="h-4 w-4" />
						{m.error_goHome()}
					</Button>
				{/if}
			</Card.Footer>
		</Card.Root>
	</div>
</div>
