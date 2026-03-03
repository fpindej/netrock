<script lang="ts">
	import { EmptyState } from '$lib/components/common';
	import * as Card from '$lib/components/ui/card';
	import { Badge } from '$lib/components/ui/badge';
	import { History } from '@lucide/svelte';
	import * as m from '$lib/paraglide/messages';
	import type { JobExecution } from '$lib/types';
	import {
		formatJobDate,
		formatJobDuration,
		getJobStatusLabel,
		getJobStatusVariant
	} from '$lib/utils/jobs';

	interface Props {
		executions: JobExecution[];
	}

	let { executions }: Props = $props();
</script>

<Card.Root>
	<Card.Header>
		<Card.Title>{m.admin_jobDetail_executionHistory()}</Card.Title>
	</Card.Header>
	<Card.Content class="p-0">
		{#if executions.length === 0}
			<EmptyState icon={History} message={m.admin_jobDetail_noHistory()} />
		{:else}
			<!-- Mobile: card list -->
			<div class="divide-y md:hidden">
				{#each executions as execution (execution.jobId)}
					<div class="space-y-1 p-4">
						<div class="flex items-center justify-between">
							<span class="text-xs text-muted-foreground">
								{formatJobDate(execution.startedAt)}
							</span>
							<Badge variant={getJobStatusVariant(execution.status)}>
								{getJobStatusLabel(execution.status)}
							</Badge>
						</div>
						{#if execution.duration}
							<p class="text-xs text-muted-foreground">
								{m.admin_jobDetail_col_duration()}: {formatJobDuration(execution.duration)}
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
									{formatJobDate(execution.startedAt)}
								</td>
								<td class="px-4 py-3 text-muted-foreground tabular-nums">
									{formatJobDuration(execution.duration)}
								</td>
								<td class="px-4 py-3">
									<Badge variant={getJobStatusVariant(execution.status)}>
										{getJobStatusLabel(execution.status)}
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
