<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import { Badge } from '$lib/components/ui/badge';
	import {
		Hash,
		User as UserIcon,
		Mail,
		Phone,
		Shield,
		CheckCircle,
		XCircle,
		AtSign
	} from '@lucide/svelte';
	import { InfoItem } from '$lib/components/profile';
	import type { AdminUser } from '$lib/types';
	import * as m from '$lib/paraglide/messages';

	interface Props {
		user: AdminUser;
	}

	let { user }: Props = $props();
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
			{user.username}
		</InfoItem>

		<InfoItem icon={Mail} label={m.admin_userDetail_email()}>
			{user.email}
		</InfoItem>

		<InfoItem icon={Phone} label={m.admin_userDetail_phone()}>
			{user.phoneNumber ?? m.admin_userDetail_notSet()}
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
