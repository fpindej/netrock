<script lang="ts">
	import * as Dialog from '$lib/components/ui/dialog';
	import { Button } from '$lib/components/ui/button';
	import { Input } from '$lib/components/ui/input';
	import { Label } from '$lib/components/ui/label';
	import * as m from '$lib/paraglide/messages';
	import { browserClient, getErrorMessage, handleMutationError } from '$lib/api';
	import { toast } from '$lib/components/ui/sonner';
	import { createFieldShakes, createCooldown } from '$lib/state';
	import { Copy, Check, Loader2 } from '@lucide/svelte';

	interface Props {
		open: boolean;
	}

	let { open = $bindable() }: Props = $props();

	type Step = 'password' | 'codes';

	let step = $state<Step>('password');
	let password = $state('');
	let isLoading = $state(false);
	let fieldErrors = $state<Record<string, string>>({});
	let generalError = $state('');
	let recoveryCodes = $state<string[]>([]);
	let codesCopied = $state(false);
	const fieldShakes = createFieldShakes();
	const cooldown = createCooldown();

	$effect(() => {
		if (open) {
			step = 'password';
			password = '';
			fieldErrors = {};
			generalError = '';
			recoveryCodes = [];
			codesCopied = false;
		}
	});

	async function handleSubmit() {
		fieldErrors = {};
		generalError = '';
		isLoading = true;

		try {
			const {
				response,
				data,
				error: apiError
			} = await browserClient.POST('/api/auth/2fa/recovery-codes', {
				body: { password }
			});

			if (response.ok && data) {
				recoveryCodes = data.recoveryCodes ?? [];
				step = 'codes';
			} else {
				handleMutationError(response, apiError, {
					cooldown,
					fallback: m.settings_twoFactor_regenerateError(),
					onValidationError(errors) {
						fieldErrors = errors;
						fieldShakes.triggerFields(Object.keys(errors));
					},
					onError() {
						generalError = getErrorMessage(apiError, m.settings_twoFactor_regenerateError());
						fieldShakes.trigger('password');
					}
				});
			}
		} catch {
			toast.error(m.settings_twoFactor_regenerateError());
		} finally {
			isLoading = false;
		}
	}

	async function copyCodes() {
		try {
			await navigator.clipboard.writeText(recoveryCodes.join('\n'));
			codesCopied = true;
			toast.success(m.settings_twoFactor_codesCopied());
			setTimeout(() => (codesCopied = false), 2000);
		} catch {
			toast.error(m.settings_twoFactor_copyFailed());
		}
	}
</script>

<Dialog.Root bind:open>
	<Dialog.Content class="sm:max-w-md">
		<Dialog.Header>
			<Dialog.Title>
				{step === 'codes'
					? m.settings_twoFactor_recoveryCodesTitle()
					: m.settings_twoFactor_regenerateTitle()}
			</Dialog.Title>
			<Dialog.Description>
				{step === 'codes'
					? m.settings_twoFactor_recoveryCodesDescription()
					: m.settings_twoFactor_regenerateDescription()}
			</Dialog.Description>
		</Dialog.Header>

		{#if step === 'password'}
			<form
				onsubmit={(e) => {
					e.preventDefault();
					handleSubmit();
				}}
			>
				<div class="grid gap-4 py-4">
					<div class="grid gap-2">
						<Label for="regenerateCodesPassword">{m.settings_twoFactor_password()}</Label>
						<Input
							id="regenerateCodesPassword"
							type="password"
							autocomplete="current-password"
							bind:value={password}
							placeholder={m.settings_twoFactor_passwordPlaceholder()}
							aria-invalid={!!fieldErrors.password || !!generalError}
							aria-describedby={fieldErrors.password || generalError
								? 'regenerateCodesPasswordError'
								: undefined}
							class={fieldShakes.class('password')}
							disabled={isLoading}
						/>
						{#if fieldErrors.password}
							<p id="regenerateCodesPasswordError" class="text-xs text-destructive">
								{fieldErrors.password}
							</p>
						{:else if generalError}
							<p id="regenerateCodesPasswordError" class="text-xs text-destructive">
								{generalError}
							</p>
						{/if}
					</div>
				</div>
				<Dialog.Footer class="flex-col-reverse gap-2 sm:flex-row">
					<Dialog.Close>
						{#snippet child({ props })}
							<Button {...props} variant="outline" disabled={isLoading}>
								{m.common_cancel()}
							</Button>
						{/snippet}
					</Dialog.Close>
					<Button type="submit" disabled={isLoading || !password || cooldown.active}>
						{#if cooldown.active}
							{m.common_waitSeconds({ seconds: cooldown.remaining })}
						{:else}
							{#if isLoading}
								<Loader2 class="me-2 h-4 w-4 animate-spin" />
							{/if}
							{m.settings_twoFactor_regenerateConfirm()}
						{/if}
					</Button>
				</Dialog.Footer>
			</form>
		{:else}
			<div class="space-y-4 py-4">
				<div class="grid grid-cols-1 gap-2 sm:grid-cols-2">
					{#each recoveryCodes as recoveryCode (recoveryCode)}
						<code class="rounded-md bg-muted px-3 py-2 text-center font-mono text-sm">
							{recoveryCode}
						</code>
					{/each}
				</div>

				<Button variant="outline" class="w-full" onclick={copyCodes}>
					{#if codesCopied}
						<Check class="me-2 h-4 w-4" />
					{:else}
						<Copy class="me-2 h-4 w-4" />
					{/if}
					{m.settings_twoFactor_copyAll()}
				</Button>
			</div>

			<Dialog.Footer>
				<Button class="w-full" onclick={() => (open = false)}>
					{m.settings_twoFactor_done()}
				</Button>
			</Dialog.Footer>
		{/if}
	</Dialog.Content>
</Dialog.Root>
