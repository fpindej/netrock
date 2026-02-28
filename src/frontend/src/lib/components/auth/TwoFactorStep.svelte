<script lang="ts">
	import { browserClient, getErrorMessage, handleMutationError } from '$lib/api';
	import { cn } from '$lib/utils';
	import { createShake, createCooldown } from '$lib/state';
	import { Button } from '$lib/components/ui/button';
	import { Input } from '$lib/components/ui/input';
	import { Label } from '$lib/components/ui/label';
	import * as Card from '$lib/components/ui/card';
	import * as m from '$lib/paraglide/messages';
	import { Loader2, ArrowLeft } from '@lucide/svelte';
	import { toast } from '$lib/components/ui/sonner';
	import { fly } from 'svelte/transition';
	import { onMount, tick } from 'svelte';

	interface Props {
		challengeToken: string;
		onSuccess: () => Promise<void>;
		onBack: () => void;
	}

	let { challengeToken, onSuccess, onBack }: Props = $props();

	onMount(async () => {
		await tick();
		document.getElementById('twoFactorCode')?.focus();
	});

	let code = $state('');
	let recoveryCode = $state('');
	let isLoading = $state(false);
	let useRecovery = $state(false);
	const shake = createShake();
	const cooldown = createCooldown();

	async function submitCode(e: Event) {
		e.preventDefault();
		if (isLoading || cooldown.active) return;
		isLoading = true;

		try {
			const { response, error: apiError } = await browserClient.POST('/api/auth/login/2fa', {
				body: { challengeToken, code }
			});

			if (response.ok) {
				await onSuccess();
			} else {
				handleMutationError(response, apiError, {
					cooldown,
					fallback: m.auth_twoFactor_invalidCode(),
					onRateLimited: () => shake.trigger(),
					onError() {
						toast.error(m.auth_login_failed(), {
							description: getErrorMessage(apiError, m.auth_twoFactor_invalidCode())
						});
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

	async function submitRecoveryCode(e: Event) {
		e.preventDefault();
		if (isLoading || cooldown.active) return;
		isLoading = true;

		try {
			const { response, error: apiError } = await browserClient.POST(
				'/api/auth/login/2fa/recovery',
				{
					body: { challengeToken, recoveryCode }
				}
			);

			if (response.ok) {
				await onSuccess();
			} else {
				handleMutationError(response, apiError, {
					cooldown,
					fallback: m.auth_twoFactor_invalidCode(),
					onRateLimited: () => shake.trigger(),
					onError() {
						toast.error(m.auth_login_failed(), {
							description: getErrorMessage(apiError, m.auth_twoFactor_invalidCode())
						});
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

<div class="sm:mx-auto sm:w-full sm:max-w-md" in:fly={{ y: 20, duration: 600, delay: 100 }}>
	<Card.Root
		class={cn(
			'border-muted/60 bg-card/50 shadow-xl backdrop-blur-sm transition-colors duration-300',
			shake.active && 'animate-shake border-destructive'
		)}
	>
		<Card.Header>
			<Card.Title class="text-center text-2xl">{m.auth_twoFactor_title()}</Card.Title>
			<Card.Description class="text-center">
				{useRecovery ? m.auth_twoFactor_recoveryDescription() : m.auth_twoFactor_description()}
			</Card.Description>
		</Card.Header>
		<Card.Content>
			{#if !useRecovery}
				<form class="space-y-6" onsubmit={submitCode}>
					<div class="grid gap-2">
						<Label for="twoFactorCode">{m.auth_twoFactor_codeLabel()}</Label>
						<Input
							id="twoFactorCode"
							type="text"
							inputmode="numeric"
							autocomplete="one-time-code"
							maxlength={6}
							placeholder={m.auth_twoFactor_codePlaceholder()}
							required
							bind:value={code}
							class="bg-background/50 text-center text-lg tracking-widest"
							aria-invalid={shake.active}
							disabled={isLoading}
						/>
					</div>

					<Button
						type="submit"
						class="w-full"
						disabled={isLoading || cooldown.active || code.length !== 6}
					>
						{#if cooldown.active}
							{m.common_waitSeconds({ seconds: cooldown.remaining })}
						{:else}
							{#if isLoading}
								<Loader2 class="me-2 h-4 w-4 animate-spin" />
							{/if}
							{m.auth_twoFactor_submit()}
						{/if}
					</Button>
				</form>

				<div class="mt-4 text-center">
					<button
						type="button"
						class="text-sm text-muted-foreground hover:text-primary hover:underline"
						onclick={() => {
							useRecovery = true;
							code = '';
						}}
					>
						{m.auth_twoFactor_useRecoveryCode()}
					</button>
				</div>
			{:else}
				<form class="space-y-6" onsubmit={submitRecoveryCode}>
					<div class="grid gap-2">
						<Label for="recoveryCode">{m.auth_twoFactor_recoveryCodeLabel()}</Label>
						<Input
							id="recoveryCode"
							type="text"
							autocomplete="off"
							placeholder={m.auth_twoFactor_recoveryCodePlaceholder()}
							required
							bind:value={recoveryCode}
							class="bg-background/50 text-center tracking-wide"
							aria-invalid={shake.active}
							disabled={isLoading}
						/>
					</div>

					<Button
						type="submit"
						class="w-full"
						disabled={isLoading || cooldown.active || !recoveryCode.trim()}
					>
						{#if cooldown.active}
							{m.common_waitSeconds({ seconds: cooldown.remaining })}
						{:else}
							{#if isLoading}
								<Loader2 class="me-2 h-4 w-4 animate-spin" />
							{/if}
							{m.auth_twoFactor_submit()}
						{/if}
					</Button>
				</form>

				<div class="mt-4 text-center">
					<button
						type="button"
						class="text-sm text-muted-foreground hover:text-primary hover:underline"
						onclick={() => {
							useRecovery = false;
							recoveryCode = '';
						}}
					>
						{m.auth_twoFactor_backToCode()}
					</button>
				</div>
			{/if}

			<div class="mt-2 text-center">
				<button
					type="button"
					class="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-primary hover:underline"
					onclick={onBack}
				>
					<ArrowLeft class="h-3 w-3" />
					{m.common_backToLogin()}
				</button>
			</div>
		</Card.Content>
	</Card.Root>
</div>
