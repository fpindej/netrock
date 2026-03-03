<script lang="ts">
	import { browserClient, getErrorMessage, handleMutationError } from '$lib/api';
	import { cn } from '$lib/utils';
	import { createShake, createCooldown, healthState } from '$lib/state';
	import { goto, invalidateAll } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { Button } from '$lib/components/ui/button';
	import { Input } from '$lib/components/ui/input';
	import { Checkbox } from '$lib/components/ui/checkbox';
	import { Label } from '$lib/components/ui/label';
	import * as Form from '$lib/components/ui/form';
	import { StatusIndicator } from '$lib/components/common';
	import * as m from '$lib/paraglide/messages';
	import { fly, scale } from 'svelte/transition';
	import { Check, Loader2 } from '@lucide/svelte';
	import { AuthShell, TwoFactorStep } from '$lib/components/auth';
	import { OAuthProviderButtons } from '$lib/components/oauth';
	import { toast } from '$lib/components/ui/sonner';
	import { loginSchema } from '$lib/schemas/auth';
	import { superForm, defaults } from 'sveltekit-superforms';
	import { zod4 as zod } from 'sveltekit-superforms/adapters';

	interface Props {
		apiUrl?: string;
	}

	let { apiUrl }: Props = $props();

	const shake = createShake();
	const cooldown = createCooldown();

	let isSuccess = $state(false);
	let isRedirecting = $state(false);
	let challengeToken = $state('');
	let requiresTwoFactor = $state(false);

	const delay = (ms: number) => new Promise((r) => setTimeout(r, ms));

	let isApiOnline = $derived(!healthState.checked || healthState.online);

	const superform = superForm(defaults(zod(loginSchema)), {
		SPA: true,
		validators: zod(loginSchema),
		async onUpdate({ form }) {
			if (!form.valid || cooldown.active) return;

			try {
				const {
					response,
					data,
					error: apiError
				} = await browserClient.POST('/api/auth/login', {
					body: {
						username: form.data.email,
						password: form.data.password,
						rememberMe: form.data.rememberMe
					}
				});

				if (response.ok && data) {
					if (data.requiresTwoFactor && data.challengeToken) {
						challengeToken = data.challengeToken;
						$formData.password = '';
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
				toast.error(m.auth_login_failed(), { description: m.auth_login_error() });
				shake.trigger();
			}
		}
	});

	const { form: formData, submitting, enhance } = superform;

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
</script>

<AuthShell cardClass={cn(shake.active && 'animate-shake border-destructive')}>
	{#snippet extras()}
		<div
			class="group absolute start-[max(1rem,env(safe-area-inset-left,0px))] bottom-[max(1rem,env(safe-area-inset-bottom,0px))] flex cursor-default items-center gap-2 rounded-lg px-2 py-1.5 text-xs text-muted-foreground/60 transition-all hover:bg-muted/50 hover:text-muted-foreground"
			title={apiUrl}
		>
			<StatusIndicator status={isApiOnline ? 'online' : 'offline'} size="sm" />
			<span class="hidden group-hover:inline">{apiUrl ?? 'API'}</span>
		</div>
	{/snippet}

	{#if requiresTwoFactor && !isSuccess}
		<TwoFactorStep {challengeToken} onSuccess={completeLogin} onBack={handleTwoFactorBack} />
	{:else if !isSuccess}
		<div
			in:fly={{ y: 20, duration: 600, delay: 100 }}
			out:scale={{ duration: 400, start: 1, opacity: 0 }}
		>
			<div class="flex flex-col gap-6">
				<div class="flex flex-col items-center gap-2 text-center">
					<h1 class="text-2xl font-bold">
						{m.auth_login_title({ name: m.app_name() })}
					</h1>
					<p class="text-sm text-balance text-muted-foreground">
						{m.auth_login_subtitle()}
					</p>
				</div>

				<form method="POST" use:enhance class="space-y-6">
					<Form.Field form={superform} name="email">
						<Form.Control>
							{#snippet children({ props })}
								<Form.Label>{m.auth_login_email()}</Form.Label>
								<Input
									{...props}
									type="email"
									autocomplete="email"
									required
									bind:value={$formData.email}
									disabled={$submitting}
								/>
							{/snippet}
						</Form.Control>
						<Form.FieldErrors />
					</Form.Field>

					<div class="grid gap-2">
						<Form.Field form={superform} name="password">
							<Form.Control>
								{#snippet children({ props })}
									<div class="flex items-center justify-between">
										<Form.Label>{m.auth_login_password()}</Form.Label>
										<a
											href={resolve('/forgot-password')}
											class="inline-flex min-h-10 items-center text-sm font-medium text-primary hover:underline"
										>
											{m.auth_login_forgotPassword()}
										</a>
									</div>
									<Input
										{...props}
										type="password"
										autocomplete="current-password"
										required
										bind:value={$formData.password}
										disabled={$submitting}
									/>
								{/snippet}
							</Form.Control>
							<Form.FieldErrors />
						</Form.Field>

						<div class="flex items-center gap-2">
							<Checkbox
								id="rememberMe"
								bind:checked={$formData.rememberMe}
								disabled={$submitting}
							/>
							<Label for="rememberMe" class="text-sm font-normal">
								{m.auth_login_rememberMe()}
							</Label>
						</div>
					</div>

					<Button
						type="submit"
						class="w-full"
						disabled={!isApiOnline || $submitting || cooldown.active}
					>
						{#if cooldown.active}
							{m.common_waitSeconds({ seconds: cooldown.remaining })}
						{:else}
							{#if $submitting}
								<Loader2 class="me-2 h-4 w-4 animate-spin" />
							{/if}
							{isApiOnline ? m.auth_login_submit() : m.auth_login_apiOffline()}
						{/if}
					</Button>
				</form>

				<OAuthProviderButtons />

				<div class="text-center text-sm">
					<span class="text-muted-foreground">{m.auth_login_noAccount()}</span>
					<a
						href={resolve('/register')}
						class="ms-1 inline-flex min-h-10 items-center font-medium text-primary hover:underline"
					>
						{m.auth_login_signUp()}
					</a>
				</div>
			</div>
		</div>
	{:else if !isRedirecting}
		<div
			class="flex flex-col items-center justify-center gap-4 py-12"
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
</AuthShell>
