<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import { Badge } from '$lib/components/ui/badge';
	import { History } from '@lucide/svelte';
	import * as m from '$lib/paraglide/messages';

	interface Execution {
		jobId?: string;
		status?: string;
		startedAt?: string | null;
		duration?: string | null;
		error?: string | null;
	}

	interface Props {
		executions: Execution[];
	}

	let { executions }: Props = $props();

	function formatDate(date: string | null | undefined): string {
		if (!date) return '-';
		return new Date(date).toLocaleString();
	}

	function formatDuration(duration: string | null | undefined): string {
		if (!duration) return '-';
		const match = duration.match(/(\d+):(\d+):(\d+)/);
		if (!match) return duration;
		const hours = parseInt(match[1]);
		const minutes = parseInt(match[2]);
		const seconds = parseInt(match[3]);
		if (hours > 0) return `${hours}h ${minutes}m ${seconds}s`;
		if (minutes > 0) return `${minutes}m ${seconds}s`;
		return `${seconds}s`;
	}

	function getStatusVariant(
		status: string | undefined
	): 'default' | 'secondary' | 'destructive' | 'outline' {
		switch (status) {
			case 'Succeeded':
				return 'default';
			case 'Failed':
				return 'destructive';
			case 'Processing':
				return 'secondary';
			default:
				return 'outline';
		}
	}

	function getStatusLabel(status: string | undefined): string {
		switch (status) {
			case 'Succeeded':
				return m.admin_jobs_status_succeeded();
			case 'Failed':
				return m.admin_jobs_status_failed();
			case 'Processing':
				return m.admin_jobs_status_running();
			default:
				return status ?? m.admin_jobs_status_idle();
		}
	}
</script>

<Card.Root>
	<Card.Header>
		<Card.Title>{m.admin_jobDetail_executionHistory()}</Card.Title>
	</Card.Header>
	<Card.Content class="p-0">
		{#if executions.length === 0}
			<div class="flex flex-col items-center justify-center py-12 text-center">
				<div class="mb-3 rounded-full bg-muted p-3">
					<History class="h-6 w-6 text-muted-foreground" />
				</div>
				<p class="text-sm text-muted-foreground">{m.admin_jobDetail_noHistory()}</p>
			</div>
		{:else}
			<!-- Mobile: card list -->
			<div class="divide-y md:hidden">
				{#each executions as execution (execution.jobId)}
					<div class="space-y-1 p-4">
						<div class="flex items-center justify-between">
							<span class="text-xs text-muted-foreground">
								{formatDate(execution.startedAt)}
							</span>
							<Badge variant={getStatusVariant(execution.status)}>
								{getStatusLabel(execution.status)}
							</Badge>
						</div>
						{#if execution.duration}
							<p class="text-xs text-muted-foreground">
								{m.admin_jobDetail_col_duration()}: {formatDuration(execution.duration)}
							</p>
						{/if}
						{#if execution.error}
							<p class="truncate text-xs text-destructive">{execution.error}</p>
						{/if}
					</div>
				{/each}
			</div>

			<!-- Desktop: table -->
			<div class="hidden overflow-x-auto md:block">
				<table class="w-full text-sm">
					<thead>
						<tr class="border-b bg-muted/50 text-start">
							<th
								class="px-4 py-3 text-start text-xs font-medium tracking-wide text-muted-foreground"
							>
								{m.admin_jobDetail_col_startedAt()}
							</th>
							<th
								class="px-4 py-3 text-start text-xs font-medium tracking-wide text-muted-foreground"
							>
								{m.admin_jobDetail_col_duration()}
							</th>
							<th
								class="px-4 py-3 text-start text-xs font-medium tracking-wide text-muted-foreground"
							>
								{m.admin_jobDetail_col_status()}
							</th>
							<th
								class="px-4 py-3 text-start text-xs font-medium tracking-wide text-muted-foreground"
							>
								{m.admin_jobDetail_col_error()}
							</th>
						</tr>
					</thead>
					<tbody>
						{#each executions as execution (execution.jobId)}
							<tr class="border-b">
								<td class="px-4 py-3 text-muted-foreground">
									{formatDate(execution.startedAt)}
								</td>
								<td class="px-4 py-3 text-muted-foreground tabular-nums">
									{formatDuration(execution.duration)}
								</td>
								<td class="px-4 py-3">
									<Badge variant={getStatusVariant(execution.status)}>
										{getStatusLabel(execution.status)}
									</Badge>
								</td>
								<td class="max-w-xs truncate px-4 py-3 text-muted-foreground">
									{execution.error ?? '-'}
								</td>
							</tr>
						{/each}
					</tbody>
				</table>
			</div>
		{/if}
	</Card.Content>
</Card.Root>
