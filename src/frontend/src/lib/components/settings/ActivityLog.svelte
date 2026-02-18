<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import { Button } from '$lib/components/ui/button';
	import { Timeline, TimelineItem, TimelineContent } from '$lib/components/ui/timeline';
	import { browserClient } from '$lib/api/client';
	import { History } from '@lucide/svelte';
	import * as m from '$lib/paraglide/messages';
	import type { AuditEvent } from '$lib/types';
	import {
		getAuditActionLabel,
		getAuditActionVariant,
		formatAuditDate,
		getAuditDescription
	} from '$lib/utils/audit';

	let events = $state<AuditEvent[]>([]);
	let loading = $state(true);
	let loadingMore = $state(false);
	let hasMore = $state(false);
	let pageNumber = $state(1);

	const pageSize = 15;

	async function loadEvents(page: number, append: boolean = false): Promise<void> {
		if (append) {
			loadingMore = true;
		} else {
			loading = true;
		}

		const { data } = await browserClient.GET('/api/users/me/audit', {
			params: {
				query: { pageNumber: page, pageSize: pageSize }
			}
		});

		if (data) {
			const newEvents = (data.items as AuditEvent[]) ?? [];
			if (append) {
				events = [...events, ...newEvents];
			} else {
				events = newEvents;
			}
			pageNumber = data.pageNumber ?? 1;
			hasMore = data.hasNextPage ?? false;
		}

		loading = false;
		loadingMore = false;
	}

	function loadMore(): void {
		loadEvents(pageNumber + 1, true);
	}

	$effect(() => {
		loadEvents(1);
	});
</script>

<Card.Root>
	<Card.Header>
		<Card.Title>{m.settings_activityLog_title()}</Card.Title>
		<Card.Description>{m.settings_activityLog_description()}</Card.Description>
	</Card.Header>
	<Card.Content>
		{#if loading}
			<div class="flex items-center justify-center py-12">
				<div
					class="h-6 w-6 animate-spin rounded-full border-2 border-primary border-t-transparent"
				></div>
			</div>
		{:else if events.length === 0}
			<div class="flex flex-col items-center justify-center py-12 text-center">
				<div class="mb-3 rounded-full bg-muted p-3">
					<History class="h-6 w-6 text-muted-foreground" />
				</div>
				<p class="text-sm text-muted-foreground">{m.settings_activityLog_empty()}</p>
			</div>
		{:else}
			<Timeline>
				{#each events as event, i (event.id)}
					<TimelineItem
						variant={getAuditActionVariant(event.action)}
						isLast={i === events.length - 1 && !hasMore}
					>
						<TimelineContent
							title={getAuditActionLabel(event.action)}
							timestamp={formatAuditDate(event.createdAt)}
							description={getAuditDescription(event.action, event.metadata)}
						/>
					</TimelineItem>
				{/each}
			</Timeline>

			{#if hasMore}
				<div class="mt-4 flex justify-center">
					<Button variant="outline" size="sm" onclick={loadMore} disabled={loadingMore}>
						{loadingMore ? m.audit_timeline_loading() : m.audit_timeline_loadMore()}
					</Button>
				</div>
			{/if}
		{/if}
	</Card.Content>
</Card.Root>
