<script lang="ts">
	import { browserClient, getErrorMessage } from '$lib/api';
	import { invalidateAll } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { onMount } from 'svelte';
	import * as m from '$lib/paraglide/messages';
	import { Button } from '$lib/components/ui/button';
	import { AuthShell } from '$lib/components/auth';
	import { fly } from 'svelte/transition';
	import { Check, CircleAlert, LoaderCircle } from '@lucide/svelte';

	let { data } = $props();

	let status = $state<'verifying' | 'success' | 'error'>('verifying');
	let errorMessage = $state('');

	onMount(() => {
		if (data.token) {
			verify();
		} else {
			status = 'error';
			errorMessage = m.auth_verifyEmail_invalidLink();
		}
	});

	async function verify() {
		try {
			const { response, error: apiError } = await browserClient.POST('/api/auth/verify-email', {
				body: { token: data.token }
			});

			if (response.ok) {
				status = 'success';
				await invalidateAll();
			} else {
				status = 'error';
				errorMessage = getErrorMessage(apiError, m.auth_verifyEmail_error());
			}
		} catch {
			status = 'error';
			errorMessage = m.auth_verifyEmail_error();
		}
	}
</script>

<svelte:head>
	<title>{m.meta_titleTemplate({ title: m.meta_verifyEmail_title() })}</title>
	<meta name="description" content={m.meta_verifyEmail_description()} />
</svelte:head>

<AuthShell>
	<div in:fly={{ y: 20, duration: 600, delay: 100 }}>
		{#if status === 'verifying'}
			<div class="flex flex-col items-center gap-4 py-8">
				<LoaderCircle class="h-10 w-10 animate-spin text-muted-foreground" />
				<h1 class="text-center text-2xl font-bold">
					{m.auth_verifyEmail_verifying()}
				</h1>
			</div>
		{:else if status === 'success'}
			<div class="flex flex-col items-center gap-4 py-4">
				<div
					class="flex h-16 w-16 items-center justify-center rounded-full bg-success text-success-foreground"
				>
					<Check class="h-8 w-8" />
				</div>
				<div class="flex flex-col items-center gap-2 text-center">
					<h1 class="text-2xl font-bold">
						{m.auth_verifyEmail_successTitle()}
					</h1>
					<p class="text-sm text-balance text-muted-foreground">
						{m.auth_verifyEmail_successDescription()}
					</p>
				</div>
				<div class="flex w-full flex-col gap-2">
					<a href={resolve('/')}>
						<Button class="w-full">{m.auth_verifyEmail_goToDashboard()}</Button>
					</a>
					<div class="text-center text-sm">
						<a
							href={resolve('/login')}
							class="inline-flex min-h-10 items-center font-medium text-primary hover:underline"
						>
							{m.auth_verifyEmail_goToLogin()}
						</a>
					</div>
				</div>
			</div>
		{:else}
			<div class="flex flex-col items-center gap-4 py-4">
				<div
					class="flex h-16 w-16 items-center justify-center rounded-full bg-destructive/10 text-destructive"
				>
					<CircleAlert class="h-8 w-8" />
				</div>
				<div class="flex flex-col items-center gap-2 text-center">
					<h1 class="text-2xl font-bold">
						{m.auth_verifyEmail_error()}
					</h1>
					<p class="text-sm text-balance text-muted-foreground">
						{errorMessage}
					</p>
				</div>
				<a
					href={resolve('/login')}
					class="inline-flex min-h-10 items-center text-sm font-medium text-primary hover:underline"
				>
					{m.common_backToLogin()}
				</a>
			</div>
		{/if}
	</div>
</AuthShell>
