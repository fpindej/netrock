<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import { CircleCheck, CircleDashed } from '@lucide/svelte';
	import * as m from '$lib/paraglide/messages';
	import type { User } from '$lib/types';

	interface Props {
		user: User | null | undefined;
	}

	let { user }: Props = $props();

	let profileComplete = $derived(!!(user?.firstName && user?.lastName));
</script>

<section>
	<Card.Root>
		<Card.Header>
			<Card.Title>{m.dashboard_status_title()}</Card.Title>
		</Card.Header>
		<Card.Content>
			<div class="grid gap-6 sm:grid-cols-2">
				<div class="flex items-center gap-3">
					{#if profileComplete}
						<CircleCheck class="size-5 shrink-0 text-success" />
					{:else}
						<CircleDashed class="size-5 shrink-0 text-muted-foreground" />
					{/if}
					<div>
						<p class="text-sm font-medium">{m.dashboard_status_profile()}</p>
						<p class="text-xs text-muted-foreground">
							{profileComplete
								? m.dashboard_status_profileComplete()
								: m.dashboard_status_profileIncomplete()}
						</p>
					</div>
				</div>

				<div class="flex items-center gap-3">
					{#if user?.emailConfirmed}
						<CircleCheck class="size-5 shrink-0 text-success" />
					{:else}
						<CircleDashed class="size-5 shrink-0 text-warning" />
					{/if}
					<div>
						<p class="text-sm font-medium">{m.dashboard_status_email()}</p>
						<p class="text-xs text-muted-foreground">
							{user?.emailConfirmed
								? m.dashboard_status_emailVerified()
								: m.dashboard_status_emailUnverified()}
						</p>
					</div>
				</div>

				<div class="flex items-center gap-3">
					{#if user?.twoFactorEnabled}
						<CircleCheck class="size-5 shrink-0 text-success" />
					{:else}
						<CircleDashed class="size-5 shrink-0 text-muted-foreground" />
					{/if}
					<div>
						<p class="text-sm font-medium">{m.dashboard_status_twoFactor()}</p>
						<p class="text-xs text-muted-foreground">
							{user?.twoFactorEnabled
								? m.dashboard_status_twoFactorEnabled()
								: m.dashboard_status_twoFactorDisabled()}
						</p>
					</div>
				</div>

				<div class="flex items-center gap-3">
					<CircleCheck class="size-5 shrink-0 text-primary" />
					<div>
						<p class="text-sm font-medium">{m.dashboard_status_roles()}</p>
						<p class="text-xs text-muted-foreground">
							{user?.roles?.length ? user.roles.join(', ') : m.profile_account_noRoles()}
						</p>
					</div>
				</div>
			</div>
		</Card.Content>
	</Card.Root>
</section>
