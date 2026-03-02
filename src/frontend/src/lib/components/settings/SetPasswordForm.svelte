<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import { Button } from '$lib/components/ui/button';
	import { Input } from '$lib/components/ui/input';
	import { Label } from '$lib/components/ui/label';
	import * as m from '$lib/paraglide/messages';
	import { browserClient, getErrorMessage, handleMutationError } from '$lib/api';
	import { toast } from '$lib/components/ui/sonner';
	import { createFieldShakes, createCooldown } from '$lib/state';

	interface Props {
		onPasswordSet: () => void;
	}

	let { onPasswordSet }: Props = $props();

	let newPassword = $state('');
	let confirmPassword = $state('');
	let isLoading = $state(false);

	let fieldErrors = $state<Record<string, string>>({});
	const fieldShakes = createFieldShakes();
	const cooldown = createCooldown();

	async function handleSubmit(e: Event) {
		e.preventDefault();
		isLoading = true;
		fieldErrors = {};

		if (newPassword !== confirmPassword) {
			fieldErrors = { confirmPassword: m.settings_changePassword_mismatch() };
			fieldShakes.triggerFields(['confirmPassword']);
			isLoading = false;
			return;
		}

		try {
			const { response, error: apiError } = await browserClient.POST('/api/auth/set-password', {
				body: { newPassword }
			});

			if (response.ok) {
				toast.success(m.settings_setPassword_success());
				onPasswordSet();
			} else {
				handleMutationError(response, apiError, {
					cooldown,
					fallback: m.settings_setPassword_error(),
					onValidationError(errors) {
						fieldErrors = errors;
						fieldShakes.triggerFields(Object.keys(errors));
						toast.error(getErrorMessage(apiError, m.settings_setPassword_error()));
					},
					onError() {
						const description = getErrorMessage(apiError, '');
						toast.error(m.settings_setPassword_error(), description ? { description } : undefined);
					}
				});
			}
		} catch {
			toast.error(m.settings_setPassword_error());
		} finally {
			isLoading = false;
		}
	}
</script>

<Card.Root class="card-hover">
	<Card.Header>
		<Card.Title>{m.settings_setPassword_title()}</Card.Title>
		<Card.Description>{m.settings_setPassword_description()}</Card.Description>
	</Card.Header>
	<Card.Content>
		<form onsubmit={handleSubmit}>
			<div class="grid gap-4">
				<div class="grid gap-2">
					<Label for="newPassword">{m.settings_changePassword_newPassword()}</Label>
					<Input
						id="newPassword"
						type="password"
						autocomplete="new-password"
						bind:value={newPassword}
						required
						minlength={6}
						class={fieldShakes.class('newPassword')}
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
					<Label for="confirmPassword">{m.settings_changePassword_confirmPassword()}</Label>
					<Input
						id="confirmPassword"
						type="password"
						autocomplete="new-password"
						bind:value={confirmPassword}
						required
						class={fieldShakes.class('confirmPassword')}
						aria-invalid={!!fieldErrors.confirmPassword}
						aria-describedby={fieldErrors.confirmPassword ? 'confirmPassword-error' : undefined}
					/>
					{#if fieldErrors.confirmPassword}
						<p id="confirmPassword-error" class="text-xs text-destructive">
							{fieldErrors.confirmPassword}
						</p>
					{/if}
				</div>

				<div class="flex flex-col gap-2 sm:flex-row sm:justify-end">
					<Button type="submit" class="w-full sm:w-auto" disabled={isLoading || cooldown.active}>
						{#if cooldown.active}
							{m.common_waitSeconds({ seconds: cooldown.remaining })}
						{:else}
							{isLoading ? m.settings_changePassword_submitting() : m.settings_setPassword_submit()}
						{/if}
					</Button>
				</div>
			</div>
		</form>
	</Card.Content>
</Card.Root>
