<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import * as AlertDialog from '$lib/components/ui/alert-dialog';
	import { Badge } from '$lib/components/ui/badge';
	import { Button } from '$lib/components/ui/button';
	import { Textarea } from '$lib/components/ui/textarea';
	import { Label } from '$lib/components/ui/label';
	import {
		Hash,
		User as UserIcon,
		Mail,
		Phone,
		Shield,
		ShieldCheck,
		ShieldOff,
		CheckCircle,
		XCircle,
		AtSign,
		Loader2,
		EyeOff
	} from '@lucide/svelte';
	import { InfoItem } from '$lib/components/profile';
	import { browserClient, handleMutationError } from '$lib/api';
	import { toast } from '$lib/components/ui/sonner';
	import { invalidateAll } from '$app/navigation';
	import type { AdminUser } from '$lib/types';
	import type { Cooldown } from '$lib/state';
	import * as m from '$lib/paraglide/messages';

	interface Props {
		user: AdminUser;
		canManage: boolean;
		canManageTwoFactor: boolean;
		piiMasked: boolean;
		cooldown: Cooldown;
	}

	let { user, canManage, canManageTwoFactor, piiMasked, cooldown }: Props = $props();

	let isVerifying = $state(false);
	let disable2faDialogOpen = $state(false);
	let isDisabling2fa = $state(false);
	let disable2faReason = $state('');

	async function verifyEmail() {
		isVerifying = true;
		const { response, error } = await browserClient.POST('/api/v1/admin/users/{id}/verify-email', {
			params: { path: { id: user.id ?? '' } }
		});
		isVerifying = false;

		if (response.ok) {
			toast.success(m.admin_userDetail_verifyEmailSuccess());
			await invalidateAll();
		} else {
			handleMutationError(response, error, {
				cooldown,
				fallback: m.admin_userDetail_verifyEmailError()
			});
		}
	}

	async function disableTwoFactor() {
		isDisabling2fa = true;
		const { response, error } = await browserClient.POST('/api/v1/admin/users/{id}/disable-2fa', {
			params: { path: { id: user.id ?? '' } },
			body: { reason: disable2faReason.trim() || null }
		});
		isDisabling2fa = false;
		disable2faDialogOpen = false;
		disable2faReason = '';

		if (response.ok) {
			toast.success(m.admin_userDetail_disable2faSuccess());
			await invalidateAll();
		} else {
			handleMutationError(response, error, {
				cooldown,
				fallback: m.admin_userDetail_disable2faError()
			});
		}
	}
</script>

<Card.Root>
	<Card.Header>
		<Card.Title>{m.admin_userDetail_accountInfo()}</Card.Title>
		<Card.Description>{m.admin_userDetail_accountInfoDescription()}</Card.Description>
	</Card.Header>
	<Card.Content class="grid gap-4 sm:grid-cols-2">
		<InfoItem icon={Hash} label={m.admin_userDetail_userId()}>
			<span class="font-mono text-xs">{user.id}</span>
		</InfoItem>

		<InfoItem icon={AtSign} label={m.admin_userDetail_username()}>
			{#if piiMasked}
				<span class="inline-flex items-center gap-1.5 text-muted-foreground italic">
					<EyeOff class="h-3.5 w-3.5" aria-hidden="true" />
					{m.admin_pii_masked()}
				</span>
			{:else}
				{user.username}
			{/if}
		</InfoItem>

		<InfoItem icon={Mail} label={m.admin_userDetail_email()}>
			{#if piiMasked}
				<span class="inline-flex items-center gap-1.5 text-muted-foreground italic">
					<EyeOff class="h-3.5 w-3.5" aria-hidden="true" />
					{m.admin_pii_masked()}
				</span>
			{:else}
				{user.email}
			{/if}
		</InfoItem>

		<InfoItem icon={Phone} label={m.admin_userDetail_phone()}>
			{#if piiMasked}
				<span class="inline-flex items-center gap-1.5 text-muted-foreground italic">
					<EyeOff class="h-3.5 w-3.5" aria-hidden="true" />
					{m.admin_pii_masked()}
				</span>
			{:else}
				{user.phoneNumber ?? m.admin_userDetail_notSet()}
			{/if}
		</InfoItem>

		<InfoItem icon={UserIcon} label={m.admin_userDetail_firstName()}>
			{user.firstName ?? m.admin_userDetail_notSet()}
		</InfoItem>

		<InfoItem icon={UserIcon} label={m.admin_userDetail_lastName()}>
			{user.lastName ?? m.admin_userDetail_notSet()}
		</InfoItem>

		<InfoItem
			icon={user.emailConfirmed ? CheckCircle : XCircle}
			label={m.admin_userDetail_emailConfirmed()}
		>
			{#if user.emailConfirmed}
				<Badge
					variant="outline"
					class="border-success/30 bg-success/10 text-success dark:border-success/30 dark:bg-success/10 dark:text-success-foreground"
				>
					{m.admin_userDetail_yes()}
				</Badge>
			{:else}
				<div class="flex items-center gap-2">
					<Badge variant="outline" class="text-muted-foreground">
						{m.admin_userDetail_no()}
					</Badge>
					{#if canManage}
						<Button
							variant="outline"
							size="sm"
							class="h-6 px-2 text-xs"
							disabled={isVerifying || cooldown.active}
							onclick={verifyEmail}
						>
							{#if isVerifying}
								<Loader2 class="me-1 h-3 w-3 animate-spin" />
							{/if}
							{m.admin_userDetail_verifyEmail()}
						</Button>
					{/if}
				</div>
			{/if}
		</InfoItem>

		<InfoItem
			icon={user.twoFactorEnabled ? ShieldCheck : ShieldOff}
			label={m.admin_userDetail_twoFactorEnabled()}
		>
			{#if user.twoFactorEnabled}
				<div class="flex items-center gap-2">
					<Badge
						variant="outline"
						class="border-success/30 bg-success/10 text-success dark:border-success/30 dark:bg-success/10 dark:text-success-foreground"
					>
						{m.admin_userDetail_yes()}
					</Badge>
					{#if canManageTwoFactor}
						<AlertDialog.Root bind:open={disable2faDialogOpen}>
							<AlertDialog.Trigger>
								{#snippet child({ props })}
									<Button
										variant="outline"
										size="sm"
										class="h-6 px-2 text-xs text-destructive hover:text-destructive"
										{...props}
									>
										{m.admin_userDetail_disable2fa()}
									</Button>
								{/snippet}
							</AlertDialog.Trigger>
							<AlertDialog.Content>
								<AlertDialog.Header>
									<AlertDialog.Title
										>{m.admin_userDetail_disable2faConfirmTitle()}</AlertDialog.Title
									>
									<AlertDialog.Description>
										{m.admin_userDetail_disable2faConfirmDescription()}
									</AlertDialog.Description>
								</AlertDialog.Header>
								<div class="grid gap-2">
									<Label for="disable-2fa-reason"
										>{m.admin_userDetail_disable2faReasonLabel()}</Label
									>
									<Textarea
										id="disable-2fa-reason"
										placeholder={m.admin_userDetail_disable2faReasonPlaceholder()}
										bind:value={disable2faReason}
										class="resize-none"
										rows={2}
									/>
								</div>
								<AlertDialog.Footer class="flex-col-reverse sm:flex-row">
									<AlertDialog.Cancel>{m.common_cancel()}</AlertDialog.Cancel>
									<AlertDialog.Action
										class="bg-destructive text-destructive-foreground shadow-sm hover:bg-destructive/90"
										disabled={isDisabling2fa || cooldown.active}
										onclick={(e: MouseEvent) => {
											e.preventDefault();
											disableTwoFactor();
										}}
									>
										{#if cooldown.active}
											{m.common_waitSeconds({ seconds: cooldown.remaining })}
										{:else}
											{#if isDisabling2fa}
												<Loader2 class="me-2 h-4 w-4 animate-spin" />
											{/if}
											{m.admin_userDetail_disable2faConfirm()}
										{/if}
									</AlertDialog.Action>
								</AlertDialog.Footer>
							</AlertDialog.Content>
						</AlertDialog.Root>
					{/if}
				</div>
			{:else}
				<Badge variant="outline" class="text-muted-foreground">
					{m.admin_userDetail_no()}
				</Badge>
			{/if}
		</InfoItem>

		<InfoItem icon={Shield} label={m.admin_userDetail_accessFailedCount()}>
			<span class="tabular-nums">{user.accessFailedCount ?? 0}</span>
		</InfoItem>
	</Card.Content>
</Card.Root>
