<script lang="ts">
	import * as Dialog from '$lib/components/ui/dialog';
	import { Button } from '$lib/components/ui/button';
	import { Input } from '$lib/components/ui/input';
	import { Label } from '$lib/components/ui/label';
	import { PhoneInput } from '$lib/components/ui/phone-input';
	import { browserClient, getErrorMessage, handleMutationError } from '$lib/api';
	import * as m from '$lib/paraglide/messages';
	import { toast } from '$lib/components/ui/sonner';
	import { Loader2 } from '@lucide/svelte';
	import { createFieldShakes, createCooldown } from '$lib/state';
	import { TurnstileWidget } from '$lib/components/auth';
	import { PUBLIC_TURNSTILE_SITE_KEY } from '$env/static/public';

	interface Props {
		open?: boolean;
		onSuccess?: (email: string) => void;
	}

	let { open = $bindable(false), onSuccess }: Props = $props();

	let email = $state('');
	let password = $state('');
	let confirmPassword = $state('');
	let firstName = $state('');
	let lastName = $state('');
	let phoneNumber = $state('');
	let captchaToken = $state('');
	let resetCaptcha: (() => void) | null = $state(null);
	let isLoading = $state(false);
	let error = $state<string | null>(null);
	let fieldErrors = $state<Record<string, string>>({});
	const fieldShakes = createFieldShakes();
	const cooldown = createCooldown();

	// ── Draft persistence (non-sensitive fields only) ──────────────────
	const DRAFT_KEY = 'register-form-draft';

	function saveDraft() {
		try {
			localStorage.setItem(DRAFT_KEY, JSON.stringify({ firstName, lastName, email, phoneNumber }));
		} catch {
			/* localStorage may be unavailable */
		}
	}

	function loadDraft() {
		try {
			const raw = localStorage.getItem(DRAFT_KEY);
			if (!raw) return;
			const draft = JSON.parse(raw);
			firstName = draft.firstName ?? '';
			lastName = draft.lastName ?? '';
			email = draft.email ?? '';
			phoneNumber = draft.phoneNumber ?? '';
		} catch {
			/* ignore parse errors */
		}
	}

	function clearDraft() {
		try {
			localStorage.removeItem(DRAFT_KEY);
		} catch {
			/* localStorage may be unavailable */
		}
	}

	// Auto-save draft while dialog is open (never persists passwords).
	// saveDraft() reads the draft fields, so $effect subscribes to their changes automatically.
	$effect(() => {
		if (open) saveDraft();
	});

	function resetForm() {
		email = '';
		password = '';
		confirmPassword = '';
		firstName = '';
		lastName = '';
		phoneNumber = '';
		captchaToken = '';
		error = null;
		fieldErrors = {};
	}

	function handleOpenChange(isOpen: boolean) {
		if (isOpen) {
			loadDraft();
		} else {
			password = '';
			confirmPassword = '';
			captchaToken = '';
			error = null;
			fieldErrors = {};
		}
	}

	async function register(e: Event) {
		e.preventDefault();
		isLoading = true;
		error = null;
		fieldErrors = {};

		if (password !== confirmPassword) {
			fieldErrors = { confirmPassword: m.auth_register_passwordMismatch() };
			fieldShakes.trigger('confirmPassword');
			isLoading = false;
			return;
		}

		try {
			const { response, error: apiError } = await browserClient.POST('/api/auth/register', {
				body: {
					email,
					password,
					captchaToken,
					firstName: firstName || undefined,
					lastName: lastName || undefined,
					phoneNumber: phoneNumber || undefined
				}
			});

			if (response.ok) {
				toast.success(m.auth_register_success());
				const registeredEmail = email;
				clearDraft();
				open = false;
				resetForm();
				onSuccess?.(registeredEmail);
			} else {
				handleMutationError(response, apiError, {
					cooldown,
					fallback: m.auth_register_failed(),
					onValidationError(errors) {
						fieldErrors = errors;
						fieldShakes.triggerFields(Object.keys(errors));
					},
					onError() {
						error = getErrorMessage(apiError, m.auth_register_failed());
					}
				});
				resetCaptcha?.();
				captchaToken = '';
			}
		} catch {
			error = m.auth_register_failed();
			resetCaptcha?.();
			captchaToken = '';
		} finally {
			isLoading = false;
		}
	}
</script>

<Dialog.Root bind:open onOpenChange={handleOpenChange}>
	<Dialog.Content class="sm:max-w-[425px]" interactOutsideBehavior="ignore">
		<Dialog.Header>
			<Dialog.Title>{m.auth_register_title()}</Dialog.Title>
			<Dialog.Description>
				{m.auth_register_description()}
			</Dialog.Description>
		</Dialog.Header>
		<form
			onsubmit={register}
			class="grid gap-4 py-4"
			aria-describedby={error ? 'register-error' : undefined}
		>
			{#if error}
				<div id="register-error" role="alert" class="text-sm font-medium text-destructive">
					{error}
				</div>
			{/if}
			<div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
				<div class="grid gap-2">
					<Label for="firstName">{m.auth_register_firstName()}</Label>
					<Input
						id="firstName"
						autocomplete="given-name"
						bind:value={firstName}
						disabled={isLoading}
						class={fieldShakes.class('firstName')}
						aria-invalid={!!fieldErrors.firstName}
					/>
					{#if fieldErrors.firstName}
						<p class="text-xs text-destructive">{fieldErrors.firstName}</p>
					{/if}
				</div>
				<div class="grid gap-2">
					<Label for="lastName">{m.auth_register_lastName()}</Label>
					<Input
						id="lastName"
						autocomplete="family-name"
						bind:value={lastName}
						disabled={isLoading}
						class={fieldShakes.class('lastName')}
						aria-invalid={!!fieldErrors.lastName}
					/>
					{#if fieldErrors.lastName}
						<p class="text-xs text-destructive">{fieldErrors.lastName}</p>
					{/if}
				</div>
			</div>
			<div class="grid gap-2">
				<Label for="email">{m.auth_register_email()}</Label>
				<Input
					id="email"
					type="email"
					autocomplete="email"
					bind:value={email}
					required
					disabled={isLoading}
					class={fieldShakes.class('email')}
					aria-invalid={!!fieldErrors.email}
				/>
				{#if fieldErrors.email}
					<p class="text-xs text-destructive">{fieldErrors.email}</p>
				{/if}
			</div>
			<div class="grid gap-2">
				<Label for="phone">{m.auth_register_phone()}</Label>
				<PhoneInput
					id="phone"
					bind:value={phoneNumber}
					disabled={isLoading}
					class={fieldShakes.class('phoneNumber')}
					aria-invalid={!!fieldErrors.phoneNumber}
				/>
				{#if fieldErrors.phoneNumber}
					<p class="text-xs text-destructive">{fieldErrors.phoneNumber}</p>
				{/if}
			</div>
			<div class="grid gap-2">
				<Label for="password">{m.auth_register_password()}</Label>
				<Input
					id="password"
					type="password"
					autocomplete="new-password"
					bind:value={password}
					required
					minlength={6}
					disabled={isLoading}
					class={fieldShakes.class('password')}
					aria-invalid={!!fieldErrors.password}
				/>
				{#if fieldErrors.password}
					<p class="text-xs text-destructive">{fieldErrors.password}</p>
				{/if}
			</div>
			<div class="grid gap-2">
				<Label for="confirmPassword">{m.auth_register_confirmPassword()}</Label>
				<Input
					id="confirmPassword"
					type="password"
					autocomplete="new-password"
					bind:value={confirmPassword}
					required
					minlength={6}
					disabled={isLoading}
					class={fieldShakes.class('confirmPassword')}
					aria-invalid={!!fieldErrors.confirmPassword}
				/>
				{#if fieldErrors.confirmPassword}
					<p class="text-xs text-destructive">{fieldErrors.confirmPassword}</p>
				{/if}
			</div>
			<TurnstileWidget
				siteKey={PUBLIC_TURNSTILE_SITE_KEY}
				onVerified={(t) => (captchaToken = t)}
				onError={() => (error = m.auth_captcha_error())}
				resetRef={(fn) => (resetCaptcha = fn)}
			/>
			<Dialog.Footer>
				<Button
					type="submit"
					disabled={isLoading || cooldown.active || !captchaToken}
					class="w-full"
				>
					{#if cooldown.active}
						{m.common_waitSeconds({ seconds: cooldown.remaining })}
					{:else}
						{#if isLoading}
							<Loader2 class="me-2 h-4 w-4 animate-spin" />
						{/if}
						{m.auth_register_submit()}
					{/if}
				</Button>
			</Dialog.Footer>
		</form>
	</Dialog.Content>
</Dialog.Root>
