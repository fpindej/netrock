<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import { resolve } from '$app/paths';
	import { routes, adminRoutes } from '$lib/config';
	import { hasAnyPermission } from '$lib/utils';
	import { UserPen, Settings, ShieldCheck, ArrowUpRight } from '@lucide/svelte';
	import * as m from '$lib/paraglide/messages';
	import type { User } from '$lib/types';

	interface Props {
		user: User | null | undefined;
	}

	let { user }: Props = $props();

	let hasAdminAccess = $derived(
		hasAnyPermission(
			user,
			Object.values(adminRoutes).map((r) => r.permission)
		)
	);
</script>

<section>
	<h4 class="mb-4 text-sm font-medium text-muted-foreground">
		{m.dashboard_quickActions()}
	</h4>
	<div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
		<a href={resolve(routes.profile)} class="group block min-h-11 text-foreground">
			<Card.Root class="h-full transition-colors hover:border-primary/50">
				<Card.Header>
					<div class="flex items-center justify-between">
						<div
							class="flex size-10 items-center justify-center rounded-lg bg-primary/10 text-primary"
						>
							<UserPen class="size-5" />
						</div>
						<ArrowUpRight
							class="size-4 text-muted-foreground opacity-0 transition-opacity group-hover:opacity-100"
						/>
					</div>
					<Card.Title class="text-base">{m.nav_profile()}</Card.Title>
					<Card.Description>{m.dashboard_quickAction_profile()}</Card.Description>
				</Card.Header>
			</Card.Root>
		</a>

		<a href={resolve(routes.settings)} class="group block min-h-11 text-foreground">
			<Card.Root class="h-full transition-colors hover:border-primary/50">
				<Card.Header>
					<div class="flex items-center justify-between">
						<div
							class="flex size-10 items-center justify-center rounded-lg bg-primary/10 text-primary"
						>
							<Settings class="size-5" />
						</div>
						<ArrowUpRight
							class="size-4 text-muted-foreground opacity-0 transition-opacity group-hover:opacity-100"
						/>
					</div>
					<Card.Title class="text-base">{m.nav_settings()}</Card.Title>
					<Card.Description>{m.dashboard_quickAction_settings()}</Card.Description>
				</Card.Header>
			</Card.Root>
		</a>

		{#if hasAdminAccess}
			<a href={resolve(adminRoutes.users.path)} class="group block min-h-11 text-foreground">
				<Card.Root class="h-full transition-colors hover:border-primary/50">
					<Card.Header>
						<div class="flex items-center justify-between">
							<div
								class="flex size-10 items-center justify-center rounded-lg bg-primary/10 text-primary"
							>
								<ShieldCheck class="size-5" />
							</div>
							<ArrowUpRight
								class="size-4 text-muted-foreground opacity-0 transition-opacity group-hover:opacity-100"
							/>
						</div>
						<Card.Title class="text-base">{m.nav_admin()}</Card.Title>
						<Card.Description>{m.dashboard_quickAction_admin()}</Card.Description>
					</Card.Header>
				</Card.Root>
			</a>
		{/if}
	</div>
</section>
