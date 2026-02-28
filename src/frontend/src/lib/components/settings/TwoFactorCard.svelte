<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import { Button } from '$lib/components/ui/button';
	import * as m from '$lib/paraglide/messages';
	import { Badge } from '$lib/components/ui/badge';
	import { ShieldCheck, ShieldOff } from '@lucide/svelte';
	import TwoFactorSetupDialog from './TwoFactorSetupDialog.svelte';
	import TwoFactorDisableDialog from './TwoFactorDisableDialog.svelte';
	import TwoFactorRecoveryCodesDialog from './TwoFactorRecoveryCodesDialog.svelte';

	interface Props {
		twoFactorEnabled: boolean;
	}

	let { twoFactorEnabled = $bindable() }: Props = $props();

	let setupDialogOpen = $state(false);
	let disableDialogOpen = $state(false);
	let regenerateDialogOpen = $state(false);
</script>

<Card.Root class="card-hover">
	<Card.Header>
		<div class="flex items-center justify-between">
			<div class="space-y-1">
				<Card.Title>{m.settings_twoFactor_title()}</Card.Title>
				<Card.Description>{m.settings_twoFactor_description()}</Card.Description>
			</div>
			{#if twoFactorEnabled}
				<Badge variant="default" class="shrink-0">
					<ShieldCheck class="me-1 h-3 w-3" />
					{m.settings_twoFactor_enabled()}
				</Badge>
			{:else}
				<Badge variant="secondary" class="shrink-0">
					<ShieldOff class="me-1 h-3 w-3" />
					{m.settings_twoFactor_disabled()}
				</Badge>
			{/if}
		</div>
	</Card.Header>
	<Card.Content>
		<div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
			<p class="text-sm text-muted-foreground">
				{twoFactorEnabled
					? m.settings_twoFactor_statusEnabled()
					: m.settings_twoFactor_statusDisabled()}
			</p>
			<div class="flex flex-wrap gap-2">
				{#if twoFactorEnabled}
					<Button variant="outline" onclick={() => (regenerateDialogOpen = true)}>
						{m.settings_twoFactor_regenerateCodes()}
					</Button>
					<Button variant="destructive" onclick={() => (disableDialogOpen = true)}>
						{m.settings_twoFactor_disable()}
					</Button>
				{:else}
					<Button onclick={() => (setupDialogOpen = true)}>
						{m.settings_twoFactor_enable()}
					</Button>
				{/if}
			</div>
		</div>
	</Card.Content>
</Card.Root>

<TwoFactorSetupDialog bind:open={setupDialogOpen} onEnabled={() => (twoFactorEnabled = true)} />
<TwoFactorDisableDialog
	bind:open={disableDialogOpen}
	onDisabled={() => (twoFactorEnabled = false)}
/>
<TwoFactorRecoveryCodesDialog bind:open={regenerateDialogOpen} />
