<script lang="ts">
	import * as Dialog from '$lib/components/ui/dialog';
	import { Button } from '$lib/components/ui/button';
	import { Input } from '$lib/components/ui/input';
	import { Label } from '$lib/components/ui/label';
	import { browserClient } from '$lib/api/client';
	import * as m from '$lib/paraglide/messages';
	import { toast } from '$lib/components/ui/sonner';
	import { Loader2 } from 'lucide-svelte';

	let { open = $bindable(false), onSuccess } = $props<{
		open?: boolean;
		onSuccess?: (email: string) => void;
	}>();

	let email = $state('');
	let password = $state('');
	let firstName = $state('');
	let lastName = $state('');
	let phoneNumber = $state('');
	let isLoading = $state(false);
	let error = $state<string | null>(null);

	async function register(e: Event) {
		e.preventDefault();
		isLoading = true;
		error = null;

		try {
			const { response, error: apiError } = await browserClient.POST('/api/auth/register', {
				body: {
					email,
					password,
					firstName: firstName || undefined,
					lastName: lastName || undefined,
					phoneNumber: phoneNumber || undefined
				}
			});

			if (response.ok) {
				toast.success(m.common_register_success());
				open = false;
				onSuccess?.(email);
				// Reset form
				email = '';
				password = '';
				firstName = '';
				lastName = '';
				phoneNumber = '';
			} else if (apiError) {
				error = apiError.detail || apiError.title || m.common_register_failed();
			} else {
				error = m.common_register_failed();
			}
		} catch (err) {
			console.error(err);
			error = m.common_register_failed();
		} finally {
			isLoading = false;
		}
	}
</script>

<Dialog.Root bind:open>
	<Dialog.Content class="sm:max-w-[425px]">
		<Dialog.Header>
			<Dialog.Title>{m.common_register_title()}</Dialog.Title>
			<Dialog.Description>
				{m.common_register_description()}
			</Dialog.Description>
		</Dialog.Header>
		<form onsubmit={register} class="grid gap-4 py-4">
			{#if error}
				<div class="text-sm font-medium text-destructive">{error}</div>
			{/if}
			<div class="grid grid-cols-2 gap-4">
				<div class="grid gap-2">
					<Label for="firstName">{m.common_register_firstName()}</Label>
					<Input id="firstName" bind:value={firstName} disabled={isLoading} />
				</div>
				<div class="grid gap-2">
					<Label for="lastName">{m.common_register_lastName()}</Label>
					<Input id="lastName" bind:value={lastName} disabled={isLoading} />
				</div>
			</div>
			<div class="grid gap-2">
				<Label for="email">{m.common_register_email()}</Label>
				<Input id="email" type="email" bind:value={email} required disabled={isLoading} />
			</div>
			<div class="grid gap-2">
				<Label for="phone">{m.common_register_phone()}</Label>
				<Input id="phone" type="tel" bind:value={phoneNumber} disabled={isLoading} />
			</div>
			<div class="grid gap-2">
				<Label for="password">{m.common_register_password()}</Label>
				<Input
					id="password"
					type="password"
					bind:value={password}
					required
					minlength={6}
					disabled={isLoading}
				/>
			</div>
			<Dialog.Footer>
				<Button type="submit" disabled={isLoading} class="w-full">
					{#if isLoading}
						<Loader2 class="me-2 h-4 w-4 animate-spin" />
					{/if}
					{m.common_register_submit()}
				</Button>
			</Dialog.Footer>
		</form>
	</Dialog.Content>
</Dialog.Root>
