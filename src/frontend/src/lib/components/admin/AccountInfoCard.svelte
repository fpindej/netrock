<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import { Badge } from '$lib/components/ui/badge';
	import { Button } from '$lib/components/ui/button';
	import {
		Hash,
		User as UserIcon,
		Mail,
		Phone,
		Shield,
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
		piiMasked: boolean;
		cooldown: Cooldown;
	}

	let { user, canManage, piiMasked, cooldown }: Props = $props();

	let isVerifying = $state(false);

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
					<EyeOff class="h-3.5 w-3.5" />
					{m.admin_pii_masked()}
				</span>
			{:else}
				{user.username}
			{/if}
		</InfoItem>

		<InfoItem icon={Mail} label={m.admin_userDetail_email()}>
			{#if piiMasked}
				<span class="inline-flex items-center gap-1.5 text-muted-foreground italic">
					<EyeOff class="h-3.5 w-3.5" />
					{m.admin_pii_masked()}
				</span>
			{:else}
				{user.email}
			{/if}
		</InfoItem>

		<InfoItem icon={Phone} label={m.admin_userDetail_phone()}>
			{#if piiMasked}
				<span class="inline-flex items-center gap-1.5 text-muted-foreground italic">
					<EyeOff class="h-3.5 w-3.5" />
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

		<InfoItem icon={Shield} label={m.admin_userDetail_accessFailedCount()}>
			<span class="tabular-nums">{user.accessFailedCount ?? 0}</span>
		</InfoItem>
	</Card.Content>
</Card.Root>
