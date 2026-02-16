<script lang="ts">
	import { browserClient, getErrorMessage, handleMutationError } from '$lib/api';
	import { cn } from '$lib/utils';
	import { createShake, createCooldown } from '$lib/state';
	import { resolve } from '$app/paths';
	import { Button } from '$lib/components/ui/button';
	import { Input } from '$lib/components/ui/input';
	import { Label } from '$lib/components/ui/label';
	import * as Card from '$lib/components/ui/card';
	import { ThemeToggle, LanguageSelector } from '$lib/components/layout';
	import * as m from '$lib/paraglide/messages';
	import { fly, scale } from 'svelte/transition';
	import { MailCheck } from '@lucide/svelte';
	import { LoginBackground } from '$lib/components/auth';
	import { toast } from '$lib/components/ui/sonner';

	let email = $state('');
	let isLoading = $state(false);
	let isSubmitted = $state(false);
	const shake = createShake();
	const cooldown = createCooldown();

	async function submit(e: Event) {
		e.preventDefault();
		if (isLoading || cooldown.active) return;

		isLoading = true;

		try {
			const { response, error: apiError } = await browserClient.POST('/api/auth/forgot-password', {
				body: { email }
			});

			if (response.ok) {
				isSubmitted = true;
			} else {
				handleMutationError(response, apiError, {
					cooldown,
					fallback: m.auth_forgotPassword_error(),
					onRateLimited: () => shake.trigger(),
					onError() {
						toast.error(m.auth_forgotPassword_error(), {
							description: getErrorMessage(apiError, m.auth_forgotPassword_error())
						});
						shake.trigger();
					}
				});
			}
		} catch {
			toast.error(m.auth_forgotPassword_error());
			shake.trigger();
		} finally {
			isLoading = false;
		}
	}
</script>

<LoginBackground>
	<div class="absolute end-4 top-4 flex gap-2">
		<LanguageSelector />
		<ThemeToggle />
	</div>

	{#if !isSubmitted}
		<div
			class="sm:mx-auto sm:w-full sm:max-w-md"
			in:fly={{ y: 20, duration: 600, delay: 100 }}
			out:scale={{ duration: 400, start: 1, opacity: 0 }}
		>
			<Card.Root
				class={cn(
					'border-muted/60 bg-card/50 shadow-xl backdrop-blur-sm transition-colors duration-300',
					shake.active && 'animate-shake border-destructive'
				)}
			>
				<Card.Header>
					<Card.Title class="text-center text-2xl">{m.auth_forgotPassword_title()}</Card.Title>
					<Card.Description class="text-center">
						{m.auth_forgotPassword_subtitle()}
					</Card.Description>
				</Card.Header>
				<Card.Content>
					<form class="space-y-6" onsubmit={submit}>
						<div class="grid gap-2">
							<Label for="email">{m.auth_forgotPassword_email()}</Label>
							<Input
								id="email"
								type="email"
								autocomplete="email"
								required
								bind:value={email}
								class="bg-background/50"
							/>
						</div>

						<Button type="submit" class="w-full" disabled={isLoading || cooldown.active}>
							{#if cooldown.active}
								{m.common_waitSeconds({ seconds: cooldown.remaining })}
							{:else if isLoading}
								{m.auth_forgotPassword_submitting()}
							{:else}
								{m.auth_forgotPassword_submit()}
							{/if}
						</Button>
					</form>
					<div class="mt-4 text-center text-sm">
						<a href={resolve('/login')} class="font-medium text-primary hover:underline">
							{m.common_backToLogin()}
						</a>
					</div>
				</Card.Content>
			</Card.Root>
		</div>
	{:else}
		<div
			class="sm:mx-auto sm:w-full sm:max-w-md"
			in:scale={{ duration: 500, delay: 400, start: 0.8, opacity: 0 }}
		>
			<Card.Root class="border-muted/60 bg-card/50 shadow-xl backdrop-blur-sm">
				<Card.Header class="items-center">
					<div
						class="mb-2 flex h-16 w-16 items-center justify-center rounded-full bg-success/10 text-success"
					>
						<MailCheck class="h-8 w-8" />
					</div>
					<Card.Title class="text-center text-2xl">
						{m.auth_forgotPassword_successTitle()}
					</Card.Title>
					<Card.Description class="text-center">
						{m.auth_forgotPassword_successDescription()}
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
		</div>
	{/if}
</LoginBackground>
