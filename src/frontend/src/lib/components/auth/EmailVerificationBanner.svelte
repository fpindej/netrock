<script lang="ts">
	import { browserClient, handleMutationError } from '$lib/api';
	import { createCooldown } from '$lib/state';
	import { Button } from '$lib/components/ui/button';
	import * as m from '$lib/paraglide/messages';
	import { toast } from '$lib/components/ui/sonner';
	import { MailWarning, X } from '@lucide/svelte';

	let isDismissed = $state(false);
	let isResending = $state(false);
	const cooldown = createCooldown();

	async function resend() {
		if (isResending || cooldown.active) return;

		isResending = true;

		try {
			const { response } = await browserClient.POST('/api/auth/resend-verification');

			if (response.ok) {
				toast.success(m.auth_emailBanner_resendSuccess());
				cooldown.start(60);
			} else {
				handleMutationError(response, undefined, {
					cooldown,
					fallback: m.auth_emailBanner_resendError()
				});
			}
		} catch {
			toast.error(m.auth_emailBanner_resendError());
		} finally {
			isResending = false;
		}
	}
</script>

{#if !isDismissed}
	<div
		class="flex items-center gap-3 border-b bg-warning/10 px-4 py-2.5 text-sm text-warning-foreground"
		role="alert"
	>
		<MailWarning class="h-4 w-4 shrink-0" />
		<span class="flex-1">{m.auth_emailBanner_message()}</span>
		<Button
			variant="ghost"
			size="sm"
			class="h-auto px-2 py-1 text-xs font-medium hover:bg-warning/20"
			disabled={isResending || cooldown.active}
			onclick={resend}
		>
			{#if cooldown.active}
				{m.common_waitSeconds({ seconds: cooldown.remaining })}
			{:else if isResending}
				{m.auth_emailBanner_resending()}
			{:else}
				{m.auth_emailBanner_resend()}
			{/if}
		</Button>
		<button
			type="button"
			class="rounded-sm p-0.5 text-warning-foreground/60 hover:text-warning-foreground"
			onclick={() => (isDismissed = true)}
			aria-label={m.common_close()}
		>
			<X class="h-4 w-4" />
		</button>
	</div>
{/if}
