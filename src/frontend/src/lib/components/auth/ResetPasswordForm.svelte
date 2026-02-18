<script lang="ts">
	import { browserClient, getErrorMessage, handleMutationError } from '$lib/api';
	import { cn } from '$lib/utils';
	import { createFieldShakes, createCooldown } from '$lib/state';
	import { invalidateAll } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { Button } from '$lib/components/ui/button';
	import { Input } from '$lib/components/ui/input';
	import { Label } from '$lib/components/ui/label';
	import * as Card from '$lib/components/ui/card';
	import { ThemeToggle, LanguageSelector } from '$lib/components/layout';
	import * as m from '$lib/paraglide/messages';
	import { fly, scale } from 'svelte/transition';
	import { Check, CircleAlert, TriangleAlert } from '@lucide/svelte';
	import { LoginBackground } from '$lib/components/auth';
	import type { User } from '$lib/types';

	interface Props {
		token: string;
		invited?: boolean;
		user?: User | null;
	}

	let { token, invited = false, user = null }: Props = $props();

	let newPassword = $state('');
	let confirmPassword = $state('');
	let isLoading = $state(false);
	let isSuccess = $state(false);
	let isError = $state(false);
	let errorMessage = $state('');
	let fieldErrors = $state<Record<string, string>>({});
	const fieldShakes = createFieldShakes();
	const cooldown = createCooldown();

	let isSigningOut = $state(false);
	let isMissingParams = $derived(!token);

	async function signOutAndContinue() {
		isSigningOut = true;
		try {
			await browserClient.POST('/api/auth/logout');
		} catch {
			// Tokens may already be expired — that's fine
		}
		await invalidateAll();
		isSigningOut = false;
	}

	async function submit(e: Event) {
		e.preventDefault();
		if (isLoading || cooldown.active) return;

		fieldErrors = {};

		if (newPassword !== confirmPassword) {
			fieldErrors = { confirmPassword: m.auth_resetPassword_mismatch() };
			fieldShakes.triggerFields(['confirmPassword']);
			return;
		}

		isLoading = true;

		try {
			const { response, error: apiError } = await browserClient.POST('/api/auth/reset-password', {
				body: { token, newPassword }
			});

			if (response.ok) {
				isSuccess = true;
			} else {
				handleMutationError(response, apiError, {
					cooldown,
					fallback: invited ? m.auth_setPassword_error() : m.auth_resetPassword_error(),
					onValidationError(errors) {
						fieldErrors = errors;
						fieldShakes.triggerFields(Object.keys(errors));
					},
					onError() {
						errorMessage = getErrorMessage(
							apiError,
							invited ? m.auth_setPassword_error() : m.auth_resetPassword_error()
						);
						isError = true;
					}
				});
			}
		} catch {
			errorMessage = invited ? m.auth_setPassword_error() : m.auth_resetPassword_error();
			isError = true;
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

	{#if user}
		<div class="sm:mx-auto sm:w-full sm:max-w-md" in:fly={{ y: 20, duration: 600, delay: 100 }}>
			<Card.Root class="border-muted/60 bg-card/50 shadow-xl backdrop-blur-sm">
				<Card.Header class="items-center">
					<div
						class="mb-2 flex h-16 w-16 items-center justify-center rounded-full bg-warning/10 text-warning"
					>
						<TriangleAlert class="h-8 w-8" />
					</div>
					<Card.Title class="text-center text-2xl">
						{invited
							? m.auth_resetPassword_alreadySignedInInvitedTitle()
							: m.auth_resetPassword_alreadySignedInTitle()}
					</Card.Title>
					<Card.Description class="text-center">
						{invited
							? m.auth_resetPassword_alreadySignedInInvitedDescription({ email: user.email ?? '' })
							: m.auth_resetPassword_alreadySignedInDescription({ email: user.email ?? '' })}
					</Card.Description>
				</Card.Header>
				<Card.Content class="space-y-3">
					<Button class="w-full" disabled={isSigningOut} onclick={signOutAndContinue}>
						{isSigningOut
							? m.auth_resetPassword_signingOut()
							: m.auth_resetPassword_signOutAndContinue()}
					</Button>
					<a href={resolve('/')} class="block">
						<Button variant="outline" class="w-full">
							{m.auth_resetPassword_goToDashboard()}
						</Button>
					</a>
				</Card.Content>
			</Card.Root>
		</div>
	{:else if isMissingParams}
		<div class="sm:mx-auto sm:w-full sm:max-w-md" in:fly={{ y: 20, duration: 600, delay: 100 }}>
			<Card.Root class="border-muted/60 bg-card/50 shadow-xl backdrop-blur-sm">
				<Card.Header class="items-center">
					<div
						class="mb-2 flex h-16 w-16 items-center justify-center rounded-full bg-destructive/10 text-destructive"
					>
						<CircleAlert class="h-8 w-8" />
					</div>
					<Card.Title class="text-center text-2xl">
						{m.auth_resetPassword_invalidLink()}
					</Card.Title>
					<Card.Description class="text-center">
						{m.auth_resetPassword_invalidLinkDescription()}
					</Card.Description>
				</Card.Header>
				<Card.Content>
					<div class="text-center text-sm">
						<a href={resolve('/forgot-password')} class="font-medium text-primary hover:underline">
							{m.auth_resetPassword_requestNew()}
						</a>
					</div>
				</Card.Content>
			</Card.Root>
		</div>
	{:else if isError}
		<div class="sm:mx-auto sm:w-full sm:max-w-md" in:fly={{ y: 20, duration: 600, delay: 100 }}>
			<Card.Root class="border-muted/60 bg-card/50 shadow-xl backdrop-blur-sm">
				<Card.Header class="items-center">
					<div
						class="mb-2 flex h-16 w-16 items-center justify-center rounded-full bg-destructive/10 text-destructive"
					>
						<CircleAlert class="h-8 w-8" />
					</div>
					<Card.Title class="text-center text-2xl">
						{m.auth_resetPassword_errorTitle()}
					</Card.Title>
					<Card.Description class="text-center">
						{errorMessage}
					</Card.Description>
				</Card.Header>
				<Card.Content>
					<div class="text-center text-sm">
						{#if invited}
							<p class="text-muted-foreground">
								{m.auth_resetPassword_errorContactAdmin()}
							</p>
						{:else}
							<a
								href={resolve('/forgot-password')}
								class="font-medium text-primary hover:underline"
							>
								{m.auth_resetPassword_requestNew()}
							</a>
						{/if}
					</div>
				</Card.Content>
			</Card.Root>
		</div>
	{:else if !isSuccess}
		<div
			class="sm:mx-auto sm:w-full sm:max-w-md"
			in:fly={{ y: 20, duration: 600, delay: 100 }}
			out:scale={{ duration: 400, start: 1, opacity: 0 }}
		>
			<Card.Root
				class="border-muted/60 bg-card/50 shadow-xl backdrop-blur-sm transition-colors duration-300"
			>
				<Card.Header>
					<Card.Title class="text-center text-2xl">
						{invited ? m.auth_setPassword_title() : m.auth_resetPassword_title()}
					</Card.Title>
					<Card.Description class="text-center">
						{invited ? m.auth_setPassword_subtitle() : m.auth_resetPassword_subtitle()}
					</Card.Description>
				</Card.Header>
				<Card.Content>
					<form class="space-y-6" onsubmit={submit}>
						<div class="grid gap-2">
							<Label for="newPassword">{m.auth_resetPassword_newPassword()}</Label>
							<Input
								id="newPassword"
								type="password"
								autocomplete="new-password"
								required
								minlength={6}
								bind:value={newPassword}
								class={cn('bg-background/50', fieldShakes.class('newPassword'))}
								aria-invalid={!!fieldErrors.newPassword}
								aria-describedby={fieldErrors.newPassword ? 'newPassword-error' : undefined}
							/>
							{#if fieldErrors.newPassword}
								<p id="newPassword-error" class="text-xs text-destructive">
									{fieldErrors.newPassword}
								</p>
							{/if}
						</div>

						<div class="grid gap-2">
							<Label for="confirmPassword">{m.auth_resetPassword_confirmPassword()}</Label>
							<Input
								id="confirmPassword"
								type="password"
								autocomplete="new-password"
								required
								bind:value={confirmPassword}
								class={cn('bg-background/50', fieldShakes.class('confirmPassword'))}
								aria-invalid={!!fieldErrors.confirmPassword}
								aria-describedby={fieldErrors.confirmPassword ? 'confirmPassword-error' : undefined}
							/>
							{#if fieldErrors.confirmPassword}
								<p id="confirmPassword-error" class="text-xs text-destructive">
									{fieldErrors.confirmPassword}
								</p>
							{/if}
						</div>

						<Button type="submit" class="w-full" disabled={isLoading || cooldown.active}>
							{#if cooldown.active}
								{m.common_waitSeconds({ seconds: cooldown.remaining })}
							{:else if isLoading}
								{invited ? m.auth_setPassword_submitting() : m.auth_resetPassword_submitting()}
							{:else}
								{invited ? m.auth_setPassword_submit() : m.auth_resetPassword_submit()}
							{/if}
						</Button>
					</form>
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
						class="mb-2 flex h-16 w-16 items-center justify-center rounded-full bg-success text-success-foreground"
					>
						<Check class="h-8 w-8" />
					</div>
					<Card.Title class="text-center text-2xl">
						{invited ? m.auth_setPassword_successTitle() : m.auth_resetPassword_successTitle()}
					</Card.Title>
					<Card.Description class="text-center">
						{invited
							? m.auth_setPassword_successDescription()
							: m.auth_resetPassword_successDescription()}
					</Card.Description>
				</Card.Header>
				<Card.Content>
					<a href={resolve('/login')}>
						<Button class="w-full">{m.auth_resetPassword_signIn()}</Button>
					</a>
				</Card.Content>
			</Card.Root>
		</div>
	{/if}
</LoginBackground>
