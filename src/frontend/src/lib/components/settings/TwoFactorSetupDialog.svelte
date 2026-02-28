<script lang="ts">
	import * as Dialog from '$lib/components/ui/dialog';
	import { Button } from '$lib/components/ui/button';
	import { Input } from '$lib/components/ui/input';
	import { Label } from '$lib/components/ui/label';
	import * as m from '$lib/paraglide/messages';
	import { browserClient, getErrorMessage, handleMutationError } from '$lib/api';
	import { toast } from '$lib/components/ui/sonner';
	import { createCooldown } from '$lib/state';
	import { Loader2, Copy, Check } from '@lucide/svelte';
	import QRCode from 'qrcode';

	interface Props {
		open: boolean;
		onEnabled: () => void;
	}

	let { open = $bindable(), onEnabled }: Props = $props();

	type Step = 'qr' | 'recovery';

	let step = $state<Step>('qr');
	let isLoading = $state(false);
	let sharedKey = $state('');
	let authenticatorUri = $state('');
	let qrDataUrl = $state('');
	let code = $state('');
	let recoveryCodes = $state<string[]>([]);
	let keyCopied = $state(false);
	let codesCopied = $state(false);
	const cooldown = createCooldown();

	$effect(() => {
		if (open) {
			step = 'qr';
			code = '';
			sharedKey = '';
			authenticatorUri = '';
			qrDataUrl = '';
			recoveryCodes = [];
			keyCopied = false;
			codesCopied = false;
			fetchSetup();
		}
	});

	async function fetchSetup() {
		isLoading = true;
		try {
			const { response, data, error: apiError } = await browserClient.POST('/api/auth/2fa/setup');

			if (response.ok && data) {
				sharedKey = data.sharedKey ?? '';
				authenticatorUri = data.authenticatorUri ?? '';
				qrDataUrl = await QRCode.toDataURL(authenticatorUri, {
					width: 256,
					margin: 2,
					color: { dark: '#000000', light: '#ffffff' }
				});
			} else {
				toast.error(m.settings_twoFactor_setupError(), {
					description: getErrorMessage(apiError, '')
				});
				open = false;
			}
		} catch {
			toast.error(m.settings_twoFactor_setupError());
			open = false;
		} finally {
			isLoading = false;
		}
	}

	async function verifyCode(e: Event) {
		e.preventDefault();
		if (isLoading || cooldown.active) return;
		isLoading = true;

		try {
			const {
				response,
				data,
				error: apiError
			} = await browserClient.POST('/api/auth/2fa/verify-setup', {
				body: { code }
			});

			if (response.ok && data) {
				recoveryCodes = data.recoveryCodes ?? [];
				step = 'recovery';
				onEnabled();
			} else {
				handleMutationError(response, apiError, {
					cooldown,
					fallback: m.settings_twoFactor_verifyError(),
					onError() {
						toast.error(m.settings_twoFactor_verifyError(), {
							description: getErrorMessage(apiError, '')
						});
					}
				});
			}
		} catch {
			toast.error(m.settings_twoFactor_verifyError());
		} finally {
			isLoading = false;
		}
	}

	async function copyKey() {
		try {
			await navigator.clipboard.writeText(sharedKey);
			keyCopied = true;
			toast.success(m.settings_twoFactor_keyCopied());
			setTimeout(() => (keyCopied = false), 2000);
		} catch {
			toast.error(m.settings_twoFactor_copyFailed());
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
				{step === 'recovery'
					? m.settings_twoFactor_recoveryCodesTitle()
					: m.settings_twoFactor_setupTitle()}
			</Dialog.Title>
			<Dialog.Description>
				{step === 'recovery'
					? m.settings_twoFactor_recoveryCodesDescription()
					: m.settings_twoFactor_setupDescription()}
			</Dialog.Description>
		</Dialog.Header>

		{#if step === 'qr'}
			<div class="space-y-4 py-4">
				{#if isLoading && !qrDataUrl}
					<div class="flex justify-center py-8">
						<Loader2 class="h-8 w-8 animate-spin text-muted-foreground" />
					</div>
				{:else}
					<div class="space-y-3">
						<p class="text-sm text-muted-foreground">{m.settings_twoFactor_scanQr()}</p>
						<div class="flex justify-center rounded-lg bg-white p-4">
							<img
								src={qrDataUrl}
								alt="Two-factor authentication setup QR code"
								class="h-48 w-48 sm:h-64 sm:w-64"
							/>
						</div>
					</div>

					<div class="space-y-2">
						<p class="text-sm text-muted-foreground">{m.settings_twoFactor_manualEntry()}</p>
						<div class="flex items-center gap-2">
							<code class="flex-1 rounded-md bg-muted px-3 py-2 font-mono text-sm break-all">
								{sharedKey}
							</code>
							<Button variant="outline" size="icon" onclick={copyKey}>
								{#if keyCopied}
									<Check class="h-4 w-4" />
								{:else}
									<Copy class="h-4 w-4" />
								{/if}
							</Button>
						</div>
					</div>

					<form onsubmit={verifyCode} class="space-y-3">
						<div class="grid gap-2">
							<Label for="setupVerifyCode">{m.settings_twoFactor_verifyCode()}</Label>
							<Input
								id="setupVerifyCode"
								type="text"
								inputmode="numeric"
								autocomplete="one-time-code"
								pattern="[0-9]{6}"
								maxlength={6}
								placeholder="000000"
								required
								bind:value={code}
								class="text-center text-lg tracking-widest"
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
							{:else if isLoading}
								<Loader2 class="me-2 h-4 w-4 animate-spin" />
								{m.settings_twoFactor_verifying()}
							{:else}
								{m.settings_twoFactor_verifySubmit()}
							{/if}
						</Button>
					</form>
				{/if}
			</div>
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
