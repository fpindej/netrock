<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import { Button } from '$lib/components/ui/button';
	import { Input } from '$lib/components/ui/input';
	import { Label } from '$lib/components/ui/label';
	import { Switch } from '$lib/components/ui/switch';
	import { ProviderIcon } from '$lib/components/oauth';
	import { browserClient, handleMutationError } from '$lib/api';
	import { toast } from '$lib/components/ui/sonner';
	import { invalidateAll } from '$app/navigation';
	import { createCooldown, createFieldShakes } from '$lib/state';
	import { Loader2 } from '@lucide/svelte';
	import * as m from '$lib/paraglide/messages';
	import type { OAuthProviderConfig } from '$lib/types';

	interface Props {
		provider: OAuthProviderConfig;
		canManage: boolean;
	}

	let { provider, canManage }: Props = $props();

	let isEnabled = $state(provider.isEnabled ?? false);
	let clientId = $state(provider.clientId ?? '');
	let clientSecret = $state('');
	let isSaving = $state(false);
	let isTesting = $state(false);
	let fieldErrors = $state<Record<string, string>>({});
	const cooldown = createCooldown();
	const fieldShakes = createFieldShakes();

	let isDirty = $derived(
		isEnabled !== (provider.isEnabled ?? false) ||
			clientId !== (provider.clientId ?? '') ||
			clientSecret !== ''
	);

	async function save() {
		isSaving = true;
		fieldErrors = {};
		const { response, error } = await browserClient.PUT(
			'/api/v1/admin/oauth-providers/{provider}',
			{
				params: { path: { provider: provider.provider ?? '' } },
				body: {
					isEnabled,
					clientId,
					clientSecret: clientSecret || undefined
				}
			}
		);
		isSaving = false;

		if (response.ok) {
			clientSecret = '';
			toast.success(m.admin_oauthProviders_saveSuccess());
			await invalidateAll();
		} else {
			handleMutationError(response, error, {
				cooldown,
				fallback: m.admin_oauthProviders_saveError(),
				onValidationError(errors) {
					fieldErrors = errors;
					fieldShakes.triggerFields(Object.keys(errors));
				}
			});
		}
	}

	let hasCredentials = $derived(!!(clientId || provider.clientId));

	async function testConnection() {
		isTesting = true;
		const { response, error } = await browserClient.POST(
			'/api/v1/admin/oauth-providers/{provider}/test',
			{
				params: { path: { provider: provider.provider ?? '' } }
			}
		);
		isTesting = false;

		if (response.ok) {
			toast.success(m.admin_oauthProviders_testConnectionSuccess());
		} else {
			handleMutationError(response, error, {
				cooldown,
				fallback: m.admin_oauthProviders_testConnectionError()
			});
		}
	}
</script>

<Card.Root>
	<Card.Header>
		<div class="flex items-center justify-between">
			<div class="flex min-w-0 items-center gap-2">
				<ProviderIcon provider={provider.provider ?? ''} class="h-5 w-5 shrink-0" />
				<Card.Title class="truncate">{provider.displayName}</Card.Title>
			</div>
			<Switch
				checked={isEnabled}
				onCheckedChange={(v) => (isEnabled = v)}
				disabled={!canManage}
				aria-label={m.admin_oauthProviders_toggleEnabled()}
			/>
		</div>
		<Card.Description>
			{isEnabled
				? m.admin_oauthProviders_providerEnabled()
				: m.admin_oauthProviders_providerDisabled()}
		</Card.Description>
	</Card.Header>
	<Card.Content class="space-y-4">
		<div class="space-y-2">
			<Label for="{provider.provider}-client-id">{m.admin_oauthProviders_clientId()}</Label>
			<Input
				id="{provider.provider}-client-id"
				bind:value={clientId}
				placeholder={m.admin_oauthProviders_clientIdPlaceholder()}
				disabled={!canManage}
				class={fieldShakes.class('clientId')}
				aria-invalid={!!fieldErrors.clientId}
				aria-describedby={fieldErrors.clientId ? `${provider.provider}-client-id-error` : undefined}
			/>
			{#if fieldErrors.clientId}
				<p id="{provider.provider}-client-id-error" class="text-xs text-destructive">
					{fieldErrors.clientId}
				</p>
			{/if}
		</div>
		<div class="space-y-2">
			<Label for="{provider.provider}-client-secret">
				{m.admin_oauthProviders_clientSecret()}
			</Label>
			<Input
				id="{provider.provider}-client-secret"
				type="password"
				bind:value={clientSecret}
				placeholder={provider.hasClientSecret
					? m.admin_oauthProviders_secretUnchanged()
					: m.admin_oauthProviders_clientSecretPlaceholder()}
				disabled={!canManage}
				class={fieldShakes.class('clientSecret')}
				aria-invalid={!!fieldErrors.clientSecret}
				aria-describedby={fieldErrors.clientSecret
					? `${provider.provider}-client-secret-error`
					: undefined}
			/>
			{#if fieldErrors.clientSecret}
				<p id="{provider.provider}-client-secret-error" class="text-xs text-destructive">
					{fieldErrors.clientSecret}
				</p>
			{/if}
		</div>
	</Card.Content>
	{#if canManage}
		<Card.Footer class="flex flex-col gap-2 sm:flex-row sm:justify-end">
			<Button
				variant="outline"
				disabled={!hasCredentials || isTesting || cooldown.active}
				onclick={testConnection}
				class="w-full sm:w-auto"
			>
				{#if isTesting}
					<Loader2 class="me-2 h-4 w-4 animate-spin" />
				{/if}
				{m.admin_oauthProviders_testConnection()}
			</Button>
			<Button
				disabled={!isDirty || isSaving || cooldown.active}
				onclick={save}
				class="w-full sm:w-auto"
			>
				{#if cooldown.active}
					{m.common_waitSeconds({ seconds: cooldown.remaining })}
				{:else}
					{#if isSaving}
						<Loader2 class="me-2 h-4 w-4 animate-spin" />
					{/if}
					{m.admin_oauthProviders_save()}
				{/if}
			</Button>
		</Card.Footer>
	{/if}
</Card.Root>
