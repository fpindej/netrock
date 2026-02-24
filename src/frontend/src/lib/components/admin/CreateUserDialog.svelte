<script lang="ts">
	import * as Dialog from '$lib/components/ui/dialog';
	import { Button } from '$lib/components/ui/button';
	import { Input } from '$lib/components/ui/input';
	import { Label } from '$lib/components/ui/label';
	import { Loader2 } from '@lucide/svelte';
	import { browserClient, handleMutationError } from '$lib/api';
	import { toast } from '$lib/components/ui/sonner';
	import { invalidateAll } from '$app/navigation';
	import { createCooldown, createFieldShakes } from '$lib/state';
	import * as m from '$lib/paraglide/messages';

	interface Props {
		open: boolean;
	}

	let { open = $bindable() }: Props = $props();

	let email = $state('');
	let firstName = $state('');
	let lastName = $state('');
	let isCreating = $state(false);
	let fieldErrors = $state<Record<string, string>>({});
	const cooldown = createCooldown();
	const fieldShakes = createFieldShakes();

	function resetForm() {
		email = '';
		firstName = '';
		lastName = '';
		fieldErrors = {};
	}

	async function handleCreate(e: Event) {
		e.preventDefault();
		if (!email.trim()) return;
		isCreating = true;
		fieldErrors = {};

		const { response, error } = await browserClient.POST('/api/v1/admin/users', {
			body: {
				email: email.trim(),
				firstName: firstName.trim() || null,
				lastName: lastName.trim() || null
			}
		});

		isCreating = false;

		if (response.ok) {
			toast.success(m.admin_users_inviteSuccess());
			resetForm();
			open = false;
			await invalidateAll();
		} else {
			handleMutationError(response, error, {
				cooldown,
				fallback: m.admin_users_inviteError(),
				onValidationError(errors) {
					fieldErrors = errors;
					fieldShakes.triggerFields(Object.keys(errors));
				}
			});
		}
	}
</script>

<Dialog.Root bind:open onOpenChange={(isOpen) => !isOpen && resetForm()}>
	<Dialog.Content>
		<Dialog.Header>
			<Dialog.Title>{m.admin_users_inviteUser()}</Dialog.Title>
			<Dialog.Description>{m.admin_users_inviteDescription()}</Dialog.Description>
		</Dialog.Header>
		<form onsubmit={handleCreate}>
			<div class="space-y-4 py-4">
				<div>
					<Label for="user-email">{m.admin_users_inviteEmail()}</Label>
					<Input
						id="user-email"
						type="email"
						bind:value={email}
						placeholder={m.admin_users_inviteEmailPlaceholder()}
						maxlength={256}
						class={fieldShakes.class('email')}
						aria-invalid={!!fieldErrors.email}
						aria-describedby={fieldErrors.email ? 'user-email-error' : undefined}
					/>
					{#if fieldErrors.email}
						<p id="user-email-error" class="mt-1 text-xs text-destructive">
							{fieldErrors.email}
						</p>
					{/if}
				</div>
				<div>
					<Label for="user-firstName">{m.admin_users_inviteFirstName()}</Label>
					<Input
						id="user-firstName"
						bind:value={firstName}
						placeholder={m.admin_users_inviteFirstNamePlaceholder()}
						maxlength={100}
						class={fieldShakes.class('firstName')}
						aria-invalid={!!fieldErrors.firstName}
						aria-describedby={fieldErrors.firstName ? 'user-firstName-error' : undefined}
					/>
					{#if fieldErrors.firstName}
						<p id="user-firstName-error" class="mt-1 text-xs text-destructive">
							{fieldErrors.firstName}
						</p>
					{/if}
				</div>
				<div>
					<Label for="user-lastName">{m.admin_users_inviteLastName()}</Label>
					<Input
						id="user-lastName"
						bind:value={lastName}
						placeholder={m.admin_users_inviteLastNamePlaceholder()}
						maxlength={100}
						class={fieldShakes.class('lastName')}
						aria-invalid={!!fieldErrors.lastName}
						aria-describedby={fieldErrors.lastName ? 'user-lastName-error' : undefined}
					/>
					{#if fieldErrors.lastName}
						<p id="user-lastName-error" class="mt-1 text-xs text-destructive">
							{fieldErrors.lastName}
						</p>
					{/if}
				</div>
			</div>
			<Dialog.Footer class="flex-col-reverse sm:flex-row">
				<Button variant="outline" type="button" onclick={() => (open = false)}>
					{m.common_cancel()}
				</Button>
				<Button type="submit" disabled={!email.trim() || isCreating || cooldown.active}>
					{#if cooldown.active}
						{m.common_waitSeconds({ seconds: cooldown.remaining })}
					{:else}
						{#if isCreating}
							<Loader2 class="me-2 h-4 w-4 animate-spin" />
						{/if}
						{m.admin_users_inviteUser()}
					{/if}
				</Button>
			</Dialog.Footer>
		</form>
	</Dialog.Content>
</Dialog.Root>
