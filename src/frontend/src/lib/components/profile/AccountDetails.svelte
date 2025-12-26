<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import { Badge } from '$lib/components/ui/badge';
	import { User, Shield } from 'lucide-svelte';
	import InfoItem from './InfoItem.svelte';
	import type { components } from '$lib/api/v1';
	import { t } from '$lib/i18n';

	type UserType = components['schemas']['MeResponse'];

	let { user }: { user: UserType | null | undefined } = $props();
</script>

<Card.Root>
	<Card.Header>
		<Card.Title>{$t('profile.accountDetails.title')}</Card.Title>
		<Card.Description>{$t('profile.accountDetails.description')}</Card.Description>
	</Card.Header>
	<Card.Content class="space-y-6">
		<InfoItem icon={User} label={$t('profile.accountDetails.userId')}>
			{user?.id}
		</InfoItem>

		<InfoItem icon={Shield} label={$t('profile.accountDetails.roles')}>
			<div class="mt-1 flex flex-wrap gap-2">
				{#each user?.roles || [] as role (role)}
					<Badge variant="secondary">{role}</Badge>
				{:else}
					<span>{$t('profile.accountDetails.noRoles')}</span>
				{/each}
			</div>
		</InfoItem>
	</Card.Content>
</Card.Root>
