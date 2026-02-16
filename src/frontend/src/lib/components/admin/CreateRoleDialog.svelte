<script lang="ts">
	import * as Dialog from '$lib/components/ui/dialog';
	import { Button } from '$lib/components/ui/button';
	import { Input } from '$lib/components/ui/input';
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

	let name = $state('');
	let description = $state('');
	let isCreating = $state(false);
	let fieldErrors = $state<Record<string, string>>({});
	const cooldown = createCooldown();
	const fieldShakes = createFieldShakes();

	function resetForm() {
		name = '';
		description = '';
		fieldErrors = {};
	}

	async function createRole() {
		if (!name.trim()) return;
		isCreating = true;
		fieldErrors = {};

		const { response, error } = await browserClient.POST('/api/v1/admin/roles', {
			body: { name: name.trim(), description: description.trim() || null }
		});

		isCreating = false;

		if (response.ok) {
			toast.success(m.admin_roles_createSuccess());
			resetForm();
			open = false;
			await invalidateAll();
		} else {
			handleMutationError(response, error, {
				cooldown,
				fallback: m.admin_roles_createError(),
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
			<Dialog.Title>{m.admin_roles_createRole()}</Dialog.Title>
			<Dialog.Description>{m.admin_roles_createRoleDescription()}</Dialog.Description>
		</Dialog.Header>
		<div class="space-y-4 py-4">
			<div>
				<label for="role-name" class="mb-1 block text-sm font-medium">
					{m.admin_roles_name()}
				</label>
				<Input
					id="role-name"
					bind:value={name}
					placeholder={m.admin_roles_namePlaceholder()}
					maxlength={50}
					class={fieldShakes.class('name')}
					aria-invalid={!!fieldErrors.name}
					aria-describedby={fieldErrors.name ? 'role-name-error' : undefined}
				/>
				{#if fieldErrors.name}
					<p id="role-name-error" class="mt-1 text-xs text-destructive">
						{fieldErrors.name}
					</p>
				{/if}
			</div>
			<div>
				<label for="role-description" class="mb-1 block text-sm font-medium">
					{m.admin_roles_descriptionLabel()}
				</label>
				<Input
					id="role-description"
					bind:value={description}
					placeholder={m.admin_roles_descriptionPlaceholder()}
					maxlength={200}
					class={fieldShakes.class('description')}
					aria-invalid={!!fieldErrors.description}
					aria-describedby={fieldErrors.description ? 'role-description-error' : undefined}
				/>
				{#if fieldErrors.description}
					<p id="role-description-error" class="mt-1 text-xs text-destructive">
						{fieldErrors.description}
					</p>
				{/if}
			</div>
		</div>
		<Dialog.Footer class="flex-col-reverse sm:flex-row">
			<Button variant="outline" onclick={() => (open = false)}>
				{m.common_cancel()}
			</Button>
			<Button disabled={!name.trim() || isCreating || cooldown.active} onclick={createRole}>
				{#if cooldown.active}
					{m.common_waitSeconds({ seconds: cooldown.remaining })}
				{:else}
					{#if isCreating}
						<Loader2 class="me-2 h-4 w-4 animate-spin" />
					{/if}
					{m.admin_roles_createRole()}
				{/if}
			</Button>
		</Dialog.Footer>
	</Dialog.Content>
</Dialog.Root>
