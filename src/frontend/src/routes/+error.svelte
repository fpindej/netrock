<script lang="ts">
	import { page } from '$app/stores';
	import { base } from '$app/paths';
</script>

<div class="flex min-h-screen flex-col justify-center bg-gray-50 py-12 sm:px-6 lg:px-8">
	<div class="text-center sm:mx-auto sm:w-full sm:max-w-md">
		<h1 class="text-9xl font-extrabold text-indigo-600">
			{$page.status}
		</h1>
		<h2 class="mt-4 text-3xl font-bold tracking-tight text-gray-900">
			{#if $page.status === 404}
				Page not found
			{:else if $page.status === 403}
				Access denied
			{:else}
				Something went wrong
			{/if}
		</h2>
		<p class="mt-2 text-base text-gray-500">
			{$page.error?.message || 'An unexpected error occurred.'}
		</p>
		<div class="mt-6">
			<!-- eslint-disable-next-line svelte/no-navigation-without-resolve -->
			<a href="{base}/" class="text-base font-medium text-indigo-600 hover:text-indigo-500">
				Go back home <span aria-hidden="true"> &rarr;</span>
			</a>
		</div>
	</div>
</div>
