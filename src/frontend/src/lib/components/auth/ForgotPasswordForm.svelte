<script lang="ts">
	import { browserClient, getErrorMessage, handleMutationError } from '$lib/api';
	import { cn } from '$lib/utils';
	import { createShake, createCooldown } from '$lib/state';
	import { resolve } from '$app/paths';
	import { Button } from '$lib/components/ui/button';
	import { Input } from '$lib/components/ui/input';
	import { Label } from '$lib/components/ui/label';
	import * as m from '$lib/paraglide/messages';
	import { fly, scale } from 'svelte/transition';
	import { MailCheck } from '@lucide/svelte';
	import { AuthShell, TurnstileWidget } from '$lib/components/auth';
	import { toast } from '$lib/components/ui/sonner';

	interface Props {
		turnstileSiteKey: string;
	}

	let { turnstileSiteKey }: Props = $props();

	let email = $state('');
	let captchaToken = $state('');
	let resetCaptcha: (() => void) | null = $state(null);
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
				body: { email, captchaToken }
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
				resetCaptcha?.();
				captchaToken = '';
			}
		} catch {
			toast.error(m.auth_forgotPassword_error());
			shake.trigger();
			resetCaptcha?.();
			captchaToken = '';
		} finally {
			isLoading = false;
		}
	}
</script>

<AuthShell cardClass={cn(shake.active && 'animate-shake border-destructive')}>
	{#if !isSubmitted}
		<div
			in:fly={{ y: 20, duration: 600, delay: 100 }}
			out:scale={{ duration: 400, start: 1, opacity: 0 }}
		>
			<div class="flex flex-col gap-6">
				<div class="flex flex-col items-center gap-2 text-center">
					<h1 class="text-2xl font-bold">{m.auth_forgotPassword_title()}</h1>
					<p class="text-sm text-balance text-muted-foreground">
						{m.auth_forgotPassword_subtitle()}
					</p>
				</div>

				<form class="space-y-6" onsubmit={submit}>
					<div class="grid gap-2">
						<Label for="email">{m.auth_forgotPassword_email()}</Label>
						<Input id="email" type="email" autocomplete="email" required bind:value={email} />
					</div>

					<TurnstileWidget
						siteKey={turnstileSiteKey}
						onVerified={(t) => (captchaToken = t)}
						onError={() => toast.error(m.auth_captcha_error())}
						resetRef={(fn) => (resetCaptcha = fn)}
					/>

					<Button
						type="submit"
						class="w-full"
						disabled={isLoading || cooldown.active || !captchaToken}
					>
						{#if cooldown.active}
							{m.common_waitSeconds({ seconds: cooldown.remaining })}
						{:else if isLoading}
							{m.auth_forgotPassword_submitting()}
						{:else}
							{m.auth_forgotPassword_submit()}
						{/if}
					</Button>
				</form>

				<div class="text-center text-sm">
					<a
						href={resolve('/login')}
						class="inline-flex min-h-10 items-center font-medium text-primary hover:underline"
					>
						{m.common_backToLogin()}
					</a>
				</div>
			</div>
		</div>
	{:else}
		<div
			class="flex flex-col items-center gap-4 py-4"
			in:scale={{ duration: 500, delay: 400, start: 0.8, opacity: 0 }}
		>
			<div
				class="flex h-16 w-16 items-center justify-center rounded-full bg-success/10 text-success"
			>
				<MailCheck class="h-8 w-8" />
			</div>
			<div class="flex flex-col items-center gap-2 text-center">
				<h1 class="text-2xl font-bold">{m.auth_forgotPassword_successTitle()}</h1>
				<p class="text-sm text-balance text-muted-foreground">
					{m.auth_forgotPassword_successDescription()}
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
</AuthShell>
