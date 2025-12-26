<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import { Badge } from '$lib/components/ui/badge';
	import { User, Shield } from 'lucide-svelte';
	import InfoItem from './InfoItem.svelte';
	import type { components } from '$lib/api/v1';

	type UserType = components['schemas']['MeResponse'];

	let { user }: { user: UserType | null | undefined } = $props();
</script>

<Card.Root>
	<Card.Header>
		<Card.Title>Account Details</Card.Title>
		<Card.Description>Manage your account settings.</Card.Description>
	</Card.Header>
	<Card.Content class="space-y-6">
		<InfoItem icon={User} label="User ID">
			{user?.id}
		</InfoItem>

		<InfoItem icon={Shield} label="Roles">
			<div class="mt-1 flex flex-wrap gap-2">
				{#each user?.roles || [] as role}
					<Badge variant="secondary">{role}</Badge>
				{:else}
					<span>No roles assigned</span>
				{/each}
			</div>
		</InfoItem>
	</Card.Content>
</Card.Root>
