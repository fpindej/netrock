<script lang="ts">
	import * as Dialog from '$lib/components/ui/dialog';
	import { Button } from '$lib/components/ui/button';
	import { Input } from '$lib/components/ui/input';
	import { Label } from '$lib/components/ui/label';
	import * as m from '$lib/paraglide/messages';
	import { browserClient, getErrorMessage, handleMutationError } from '$lib/api';
	import { toast } from '$lib/components/ui/sonner';
	import { createFieldShakes, createCooldown } from '$lib/state';

	interface Props {
		open: boolean;
		onDisabled: () => void;
	}

	let { open = $bindable(), onDisabled }: Props = $props();

	let password = $state('');
	let isLoading = $state(false);
	let fieldErrors = $state<Record<string, string>>({});
	let generalError = $state('');
	const fieldShakes = createFieldShakes();
	const cooldown = createCooldown();

	function handleOpenChange(isOpen: boolean) {
		if (isOpen) {
			password = '';
			fieldErrors = {};
			generalError = '';
		}
	}

	async function handleSubmit() {
		fieldErrors = {};
		generalError = '';
		isLoading = true;

		try {
			const { response, error: apiError } = await browserClient.POST('/api/auth/2fa/disable', {
				body: { password }
			});

			if (response.ok) {
				open = false;
				toast.success(m.settings_twoFactor_disableSuccess());
				onDisabled();
			} else {
				handleMutationError(response, apiError, {
					cooldown,
					fallback: m.settings_twoFactor_disableError(),
					onValidationError(errors) {
						fieldErrors = errors;
						fieldShakes.triggerFields(Object.keys(errors));
					},
					onError() {
						generalError = getErrorMessage(apiError, m.settings_twoFactor_disableError());
						fieldShakes.trigger('password');
					}
				});
			}
		} catch {
			toast.error(m.settings_twoFactor_disableError());
		} finally {
			isLoading = false;
		}
	}
</script>

<Dialog.Root bind:open onOpenChange={handleOpenChange}>
	<Dialog.Content class="sm:max-w-md">
		<Dialog.Header>
			<Dialog.Title>{m.settings_twoFactor_disableTitle()}</Dialog.Title>
			<Dialog.Description>
				{m.settings_twoFactor_disableDescription()}
			</Dialog.Description>
		</Dialog.Header>
		<form
			onsubmit={(e) => {
				e.preventDefault();
				handleSubmit();
			}}
		>
			<div class="grid gap-4 py-4">
				<div class="grid gap-2">
					<Label for="disableTwoFactorPassword">{m.settings_twoFactor_password()}</Label>
					<Input
						id="disableTwoFactorPassword"
						type="password"
						autocomplete="current-password"
						bind:value={password}
						placeholder={m.settings_twoFactor_passwordPlaceholder()}
						aria-invalid={!!fieldErrors.password || !!generalError}
						aria-describedby={fieldErrors.password || generalError
							? 'disableTwoFactorPasswordError'
							: undefined}
						class={fieldShakes.class('password')}
						disabled={isLoading}
					/>
					{#if fieldErrors.password}
						<p id="disableTwoFactorPasswordError" class="text-xs text-destructive">
							{fieldErrors.password}
						</p>
					{:else if generalError}
						<p id="disableTwoFactorPasswordError" class="text-xs text-destructive">
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
				<Button
					type="submit"
					variant="destructive"
					disabled={isLoading || !password || cooldown.active}
				>
					{cooldown.active
						? m.common_waitSeconds({ seconds: cooldown.remaining })
						: m.settings_twoFactor_disableConfirm()}
				</Button>
			</Dialog.Footer>
		</form>
	</Dialog.Content>
</Dialog.Root>
