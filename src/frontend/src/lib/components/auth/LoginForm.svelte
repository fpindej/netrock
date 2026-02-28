<script lang="ts">
	import { browserClient, getErrorMessage, handleMutationError } from '$lib/api';
	import { cn } from '$lib/utils';
	import { createShake, createCooldown, healthState } from '$lib/state';
	import { goto, invalidateAll } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { Button } from '$lib/components/ui/button';
	import { Checkbox } from '$lib/components/ui/checkbox';
	import { Input } from '$lib/components/ui/input';
	import { Label } from '$lib/components/ui/label';
	import * as Card from '$lib/components/ui/card';
	import { ThemeToggle, LanguageSelector } from '$lib/components/layout';
	import { StatusIndicator } from '$lib/components/common';
	import * as m from '$lib/paraglide/messages';
	import { fly, scale } from 'svelte/transition';
	import { Check, Loader2 } from '@lucide/svelte';
	import { LoginBackground, RegisterDialog, TwoFactorStep } from '$lib/components/auth';
	import { toast } from '$lib/components/ui/sonner';

	interface Props {
		apiUrl?: string;
		turnstileSiteKey: string;
	}

	let { apiUrl, turnstileSiteKey }: Props = $props();

	let email = $state('');
	let password = $state('');
	let rememberMe = $state(false);
	let isLoading = $state(false);
	let isSuccess = $state(false);
	let isRedirecting = $state(false);
	const shake = createShake();
	const cooldown = createCooldown();
	let isRegisterOpen = $state(false);

	// 2FA challenge state
	let challengeToken = $state('');
	let requiresTwoFactor = $state(false);

	const delay = (ms: number) => new Promise((resolve) => setTimeout(resolve, ms));

	// Assume online until the first health check completes — prevents a flash
	// of "API is offline" on hard navigations where the polling restarts.
	let isApiOnline = $derived(!healthState.checked || healthState.online);

	function onRegisterSuccess(newEmail: string) {
		email = newEmail;
		password = '';
	}

	async function completeLogin() {
		isSuccess = true;
		await delay(1500);
		isRedirecting = true;
		await delay(500);
		await invalidateAll();
		await goto(resolve('/'));
	}

	function handleTwoFactorBack() {
		requiresTwoFactor = false;
		challengeToken = '';
	}

	async function login(e: Event) {
		e.preventDefault();
		if (isLoading || cooldown.active) return;

		isLoading = true;

		try {
			const {
				response,
				data,
				error: apiError
			} = await browserClient.POST('/api/auth/login', {
				body: { username: email, password, rememberMe }
			});

			if (response.ok && data) {
				if (data.requiresTwoFactor && data.challengeToken) {
					challengeToken = data.challengeToken;
					requiresTwoFactor = true;
				} else {
					await completeLogin();
				}
			} else {
				handleMutationError(response, apiError, {
					cooldown,
					fallback: m.auth_login_error(),
					onRateLimited: () => shake.trigger(),
					onError() {
						const detail = getErrorMessage(apiError, '');
						const isLocked = detail.includes('temporarily locked');
						const errorMessage =
							response.status === 401
								? isLocked
									? m.auth_login_accountLocked()
									: getErrorMessage(apiError, m.auth_login_invalidCredentials())
								: getErrorMessage(apiError, m.auth_login_error());
						toast.error(m.auth_login_failed(), { description: errorMessage });
						shake.trigger();
					}
				});
			}
		} catch {
			toast.error(m.auth_login_failed(), {
				description: m.auth_login_error()
			});
			shake.trigger();
		} finally {
			isLoading = false;
		}
	}
</script>

<LoginBackground>
	<div
		class="absolute end-[max(1rem,env(safe-area-inset-right,0px))] top-[max(1rem,env(safe-area-inset-top,0px))] flex gap-2"
	>
		<LanguageSelector />
		<ThemeToggle />
	</div>

	<!-- Subtle API status indicator for debugging -->
	<div
		class="group absolute start-[max(1rem,env(safe-area-inset-left,0px))] bottom-[max(1rem,env(safe-area-inset-bottom,0px))] flex cursor-default items-center gap-2 rounded-lg px-2 py-1.5 text-xs text-muted-foreground/60 transition-all hover:bg-muted/50 hover:text-muted-foreground"
		title={apiUrl}
	>
		<StatusIndicator status={isApiOnline ? 'online' : 'offline'} size="sm" />
		<span class="hidden group-hover:inline">{apiUrl ?? 'API'}</span>
	</div>

	{#if requiresTwoFactor && !isSuccess}
		<TwoFactorStep
			{challengeToken}
			useCookies={true}
			onSuccess={completeLogin}
			onBack={handleTwoFactorBack}
		/>
	{:else if !isSuccess}
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
					<Card.Title class="text-center text-2xl"
						>{m.auth_login_title({ name: m.app_name() })}</Card.Title
					>
					<Card.Description class="text-center">{m.auth_login_subtitle()}</Card.Description>
				</Card.Header>
				<Card.Content>
					<form class="space-y-6" onsubmit={login}>
						<div class="grid gap-2">
							<Label for="email">{m.auth_login_email()}</Label>
							<Input
								id="email"
								type="email"
								autocomplete="email"
								required
								bind:value={email}
								class="bg-background/50"
								aria-invalid={shake.active}
								disabled={isLoading}
							/>
						</div>

						<div class="grid gap-2">
							<div class="flex items-center justify-between">
								<Label for="password">{m.auth_login_password()}</Label>
								<a
									href={resolve('/forgot-password')}
									class="text-sm font-medium text-primary hover:underline"
								>
									{m.auth_login_forgotPassword()}
								</a>
							</div>
							<Input
								id="password"
								type="password"
								autocomplete="current-password"
								required
								bind:value={password}
								class="bg-background/50"
								aria-invalid={shake.active}
								disabled={isLoading}
							/>

							<div class="flex items-center gap-2">
								<Checkbox id="rememberMe" bind:checked={rememberMe} disabled={isLoading} />
								<Label for="rememberMe" class="text-sm font-normal">
									{m.auth_login_rememberMe()}
								</Label>
							</div>
						</div>

						<Button
							type="submit"
							class="w-full"
							disabled={!isApiOnline || isLoading || cooldown.active}
						>
							{#if cooldown.active}
								{m.common_waitSeconds({ seconds: cooldown.remaining })}
							{:else}
								{#if isLoading}
									<Loader2 class="me-2 h-4 w-4 animate-spin" />
								{/if}
								{isApiOnline ? m.auth_login_submit() : m.auth_login_apiOffline()}
							{/if}
						</Button>
					</form>
					<div class="mt-4 text-center text-sm">
						<span class="text-muted-foreground">{m.auth_login_noAccount()}</span>
						<button
							type="button"
							class="ms-1 inline-flex min-h-10 items-center font-medium text-primary hover:underline"
							onclick={() => (isRegisterOpen = true)}
						>
							{m.auth_login_signUp()}
						</button>
					</div>
				</Card.Content>
			</Card.Root>
		</div>
	{:else if !isRedirecting}
		<div
			class="flex flex-col items-center justify-center gap-4"
			in:scale={{ duration: 500, delay: 400, start: 0.8, opacity: 0 }}
			out:scale={{ duration: 500, start: 1.2, opacity: 0 }}
		>
			<div
				class="flex h-24 w-24 items-center justify-center rounded-full bg-success text-success-foreground shadow-2xl"
			>
				<Check class="h-12 w-12" />
			</div>
			<h2 class="text-3xl font-bold tracking-tight text-foreground">
				{m.auth_login_success()}
			</h2>
		</div>
	{/if}
</LoginBackground>

<RegisterDialog bind:open={isRegisterOpen} onSuccess={onRegisterSuccess} {turnstileSiteKey} />
