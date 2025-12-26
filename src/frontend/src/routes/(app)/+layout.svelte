<script lang="ts">
	import Header from '$lib/components/layout/Header.svelte';
	import Sidebar from '$lib/components/layout/Sidebar.svelte';

	let { children, data } = $props();
</script>

<div class="grid min-h-screen w-full md:grid-cols-[220px_1fr] lg:grid-cols-[280px_1fr]">
	<div class="hidden border-r bg-muted/40 md:block">
		<Sidebar class="h-full" user={data.user} />
	</div>
	<div class="flex flex-col">
		<Header user={data.user} />
		<main class="flex flex-1 flex-col gap-4 p-4 lg:gap-6 lg:p-6">
			{@render children()}
		</main>
	</div>
</div>
