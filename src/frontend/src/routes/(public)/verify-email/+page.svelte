<script lang="ts">
	import { browserClient, getErrorMessage } from '$lib/api';
	import { invalidateAll } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { onMount } from 'svelte';
	import * as m from '$lib/paraglide/messages';
	import * as Card from '$lib/components/ui/card';
	import { Button } from '$lib/components/ui/button';
	import { LoginBackground } from '$lib/components/auth';
	import { ThemeToggle, LanguageSelector } from '$lib/components/layout';
	import { fly } from 'svelte/transition';
	import { Check, CircleAlert, LoaderCircle } from '@lucide/svelte';

	let { data } = $props();

	let status = $state<'verifying' | 'success' | 'error'>('verifying');
	let errorMessage = $state('');

	onMount(() => {
		if (data.email && data.token) {
			verify();
		} else {
			status = 'error';
			errorMessage = m.auth_verifyEmail_invalidLink();
		}
	});

	async function verify() {
		try {
			const { response, error: apiError } = await browserClient.POST('/api/auth/verify-email', {
				body: { email: data.email, token: data.token }
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

<LoginBackground>
	<div class="absolute end-4 top-4 flex gap-2">
		<LanguageSelector />
		<ThemeToggle />
	</div>

	<div class="sm:mx-auto sm:w-full sm:max-w-md" in:fly={{ y: 20, duration: 600, delay: 100 }}>
		{#if status === 'verifying'}
			<Card.Root class="border-muted/60 bg-card/50 shadow-xl backdrop-blur-sm">
				<Card.Header class="items-center">
					<div class="mb-2 flex h-16 w-16 items-center justify-center">
						<LoaderCircle class="h-10 w-10 animate-spin text-muted-foreground" />
					</div>
					<Card.Title class="text-center text-2xl">
						{m.auth_verifyEmail_verifying()}
					</Card.Title>
				</Card.Header>
			</Card.Root>
		{:else if status === 'success'}
			<Card.Root class="border-muted/60 bg-card/50 shadow-xl backdrop-blur-sm">
				<Card.Header class="items-center">
					<div
						class="mb-2 flex h-16 w-16 items-center justify-center rounded-full bg-success text-success-foreground"
					>
						<Check class="h-8 w-8" />
					</div>
					<Card.Title class="text-center text-2xl">
						{m.auth_verifyEmail_successTitle()}
					</Card.Title>
					<Card.Description class="text-center">
						{m.auth_verifyEmail_successDescription()}
					</Card.Description>
				</Card.Header>
				<Card.Content class="flex flex-col gap-2">
					<a href={resolve('/')}>
						<Button class="w-full">{m.auth_verifyEmail_goToDashboard()}</Button>
					</a>
					<div class="text-center text-sm">
						<a href={resolve('/login')} class="font-medium text-primary hover:underline">
							{m.auth_verifyEmail_goToLogin()}
						</a>
					</div>
				</Card.Content>
			</Card.Root>
		{:else}
			<Card.Root class="border-muted/60 bg-card/50 shadow-xl backdrop-blur-sm">
				<Card.Header class="items-center">
					<div
						class="mb-2 flex h-16 w-16 items-center justify-center rounded-full bg-destructive/10 text-destructive"
					>
						<CircleAlert class="h-8 w-8" />
					</div>
					<Card.Title class="text-center text-2xl">
						{m.auth_verifyEmail_error()}
					</Card.Title>
					<Card.Description class="text-center">
						{errorMessage}
					</Card.Description>
				</Card.Header>
				<Card.Content>
					<div class="text-center text-sm">
						<a href={resolve('/login')} class="font-medium text-primary hover:underline">
							{m.common_backToLogin()}
						</a>
					</div>
				</Card.Content>
			</Card.Root>
		{/if}
	</div>
</LoginBackground>
