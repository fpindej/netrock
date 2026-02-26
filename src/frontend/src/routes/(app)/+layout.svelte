<script lang="ts">
	import { Header, Sidebar } from '$lib/components/layout';
	import { EmailVerificationBanner } from '$lib/components/auth';
	import { page } from '$app/state';
	import { invalidateAll } from '$app/navigation';
	import { onMount } from 'svelte';
	import { initSidebar, sidebarState, healthState } from '$lib/state';

	let { children, data } = $props();

	let collapsed = $derived(sidebarState.collapsed);

	onMount(() => {
		initSidebar();
	});

	// When health polling detects the backend went down, re-run server loads.
	// The (app) layout.server.ts will throw 503, showing the error page with
	// auto-recovery. Only trigger on a confirmed online→offline transition.
	let wasOnline = false;
	$effect(() => {
		if (healthState.checked && wasOnline && !healthState.online) {
			invalidateAll();
		}
		if (healthState.checked) wasOnline = healthState.online;
	});
</script>

<div
	class="grid h-dvh w-full overflow-x-hidden transition-[grid-template-columns] duration-300 md:grid-cols-[var(--sidebar-width)_1fr]"
	style="--sidebar-width: {collapsed
		? 'var(--sidebar-width-collapsed)'
		: 'var(--sidebar-width-md)'};"
>
	<div class="hidden border-e bg-muted/40 md:block">
		<Sidebar class="h-full" user={data.user} />
	</div>
	<div class="flex flex-col overflow-hidden">
		<Header user={data.user} />
		{#if !data.user.emailConfirmed}
			<EmailVerificationBanner />
		{/if}
		<main
			class="flex flex-1 flex-col gap-4 overflow-y-auto overscroll-contain p-4 pb-[max(4rem,calc(env(safe-area-inset-bottom,0px)+2rem))] lg:gap-6 lg:p-6 lg:pb-[max(4rem,calc(env(safe-area-inset-bottom,0px)+2rem))]"
		>
			{#key page.url.pathname}
				<div
					class="mx-auto w-full max-w-7xl motion-safe:duration-300 motion-safe:animate-in motion-safe:fade-in motion-safe:slide-in-from-bottom-4"
				>
					{@render children()}
				</div>
			{/key}
		</main>
	</div>
</div>
