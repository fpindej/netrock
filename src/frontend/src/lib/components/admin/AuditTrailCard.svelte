<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import * as Dialog from '$lib/components/ui/dialog';
	import { Badge } from '$lib/components/ui/badge';
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
		formatAuditDateFull,
		getAuditDescription
	} from '$lib/utils/audit';

	interface Props {
		userId: string;
	}

	let { userId }: Props = $props();

	let events = $state<AuditEvent[]>([]);
	let loading = $state(true);
	let loadingMore = $state(false);
	let hasMore = $state(false);
	let pageNumber = $state(1);
	let selectedEvent = $state<AuditEvent | null>(null);
	let dialogOpen = $state(false);

	const pageSize = 15;

	function getBadgeVariant(
		action: string | undefined
	): 'default' | 'success' | 'destructive' | 'warning' | 'outline' {
		return getAuditActionVariant(action);
	}

	async function loadEvents(page: number, append: boolean = false): Promise<void> {
		if (append) {
			loadingMore = true;
		} else {
			loading = true;
		}

		const { data } = await browserClient.GET('/api/v1/admin/users/{id}/audit', {
			params: {
				path: { id: userId },
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

	function openDetail(event: AuditEvent): void {
		selectedEvent = event;
		dialogOpen = true;
	}

	function formatMetadata(metadata: string | null | undefined): string | null {
		if (!metadata) return null;
		try {
			const parsed = JSON.parse(metadata);
			return JSON.stringify(parsed, null, 2);
		} catch {
			return metadata;
		}
	}

	$effect(() => {
		// loadEvents reads userId synchronously (in params), so $effect tracks it automatically.
		loadEvents(1);
	});
</script>

<Card.Root>
	<Card.Header>
		<Card.Title>{m.audit_trail_title()}</Card.Title>
		<Card.Description>{m.audit_trail_description()}</Card.Description>
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
				<p class="text-sm text-muted-foreground">{m.audit_trail_empty()}</p>
			</div>
		{:else}
			<Timeline>
				{#each events as event, i (event.id)}
					<TimelineItem
						variant={getAuditActionVariant(event.action)}
						isLast={i === events.length - 1 && !hasMore}
					>
						<button
							type="button"
							class="w-full rounded-md px-2 py-1 text-start transition-colors hover:bg-muted/50"
							onclick={() => openDetail(event)}
						>
							<TimelineContent
								title={getAuditActionLabel(event.action)}
								timestamp={formatAuditDate(event.createdAt)}
								description={getAuditDescription(event.action, event.metadata)}
							/>
						</button>
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

<Dialog.Root bind:open={dialogOpen}>
	<Dialog.Content class="max-w-md">
		<Dialog.Header>
			<Dialog.Title>{m.audit_detail_title()}</Dialog.Title>
		</Dialog.Header>

		{#if selectedEvent}
			<div class="space-y-4">
				<div class="flex items-center justify-between">
					<span class="text-sm text-muted-foreground">{m.audit_detail_action()}</span>
					<Badge variant={getBadgeVariant(selectedEvent.action)}>
						{getAuditActionLabel(selectedEvent.action)}
					</Badge>
				</div>

				<div class="flex items-center justify-between">
					<span class="text-sm text-muted-foreground">{m.audit_detail_timestamp()}</span>
					<span class="text-sm">{formatAuditDateFull(selectedEvent.createdAt)}</span>
				</div>

				{#if selectedEvent.targetEntityType}
					<div class="flex items-center justify-between">
						<span class="text-sm text-muted-foreground">{m.audit_detail_targetType()}</span>
						<span class="text-sm">{selectedEvent.targetEntityType}</span>
					</div>
				{/if}

				{#if selectedEvent.targetEntityId}
					<div class="flex items-center justify-between">
						<span class="text-sm text-muted-foreground">{m.audit_detail_targetId()}</span>
						<span class="font-mono text-xs">{selectedEvent.targetEntityId}</span>
					</div>
				{/if}

				{#if selectedEvent.metadata}
					<div>
						<span class="text-sm text-muted-foreground">{m.audit_detail_metadata()}</span>
						<pre class="mt-1 overflow-x-auto rounded-md bg-muted p-2 text-xs">{formatMetadata(
								selectedEvent.metadata
							)}</pre>
					</div>
				{/if}

				<div class="flex items-center justify-between border-t pt-4">
					<span class="text-sm text-muted-foreground">{m.audit_detail_eventId()}</span>
					<span class="font-mono text-xs">{selectedEvent.id}</span>
				</div>
			</div>
		{/if}
	</Dialog.Content>
</Dialog.Root>
