<script lang="ts">
	import { client } from '$lib/api/client';
	import { base } from '$app/paths';
	import { goto, invalidateAll } from '$app/navigation';
	import { Button } from '$lib/components/ui/button';

	let { user } = $props();
</script>

<!-- eslint-disable svelte/no-navigation-without-resolve -->
<nav class="bg-white shadow-sm">
	<div class="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
		<div class="flex h-16 justify-between">
			<div class="flex">
				<div class="flex flex-shrink-0 items-center">
					<a href="{base}/" class="text-xl font-bold text-indigo-600">MyProject</a>
				</div>
			</div>
			<div class="flex items-center">
				{#if user}
					<span class="mr-4 text-sm text-gray-700">Hello, {user.username || 'User'}</span>
					<div class="w-24">
						<Button
							onclick={async () => {
								await client.POST('/api/Auth/logout');
								await invalidateAll();
								await goto('/login');
							}}
						>
							Logout
						</Button>
					</div>
				{/if}
			</div>
		</div>
	</div>
</nav>
