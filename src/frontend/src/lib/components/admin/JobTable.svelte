<script lang="ts">
	import { Badge } from '$lib/components/ui/badge';
	import { Clock } from '@lucide/svelte';
	import { resolve } from '$app/paths';
	import * as m from '$lib/paraglide/messages';
	import type { Job } from '$lib/types';
	import { formatJobDate, getJobStatusLabel, getJobStatusVariant } from '$lib/utils/jobs';

	interface Props {
		jobs: Job[];
	}

	let { jobs }: Props = $props();

	function formatDate(date: string | null | undefined): string {
		return formatJobDate(date, m.admin_jobs_never());
	}
</script>

{#if jobs.length === 0}
	<div class="flex flex-col items-center justify-center py-12 text-center">
		<div class="mb-3 rounded-full bg-muted p-3">
			<Clock class="h-6 w-6 text-muted-foreground" />
		</div>
		<p class="text-sm text-muted-foreground">{m.admin_jobs_noJobs()}</p>
	</div>
{:else}
	<!-- Mobile: card list -->
	<div class="divide-y md:hidden">
		{#each jobs as job (job.id)}
			<!-- eslint-disable svelte/no-navigation-without-resolve -- href is pre-resolved -->
			<a
				href={resolve(`/admin/jobs/${job.id}`)}
				class="block p-4 transition-colors hover:bg-muted/50"
			>
				<div class="mb-2 flex items-center justify-between">
					<span class="text-sm font-medium">{job.id}</span>
					<Badge variant={getJobStatusVariant(job.lastStatus, job.isPaused)}>
						{getJobStatusLabel(job.lastStatus, job.isPaused)}
					</Badge>
				</div>
				<div class="grid grid-cols-2 gap-1 text-xs text-muted-foreground">
					<span>{m.admin_jobs_col_schedule()}: {job.cron}</span>
					<span>{m.admin_jobs_col_lastRun()}: {formatDate(job.lastExecution)}</span>
				</div>
			</a>
		{/each}
	</div>

	<!-- Desktop: table -->
	<div class="hidden overflow-x-auto md:block">
		<table class="w-full text-sm">
			<thead>
				<tr class="border-b bg-muted/50 text-start">
					<th class="px-4 py-3 text-start text-xs font-medium tracking-wide text-muted-foreground">
						{m.admin_jobs_col_name()}
					</th>
					<th class="px-4 py-3 text-start text-xs font-medium tracking-wide text-muted-foreground">
						{m.admin_jobs_col_schedule()}
					</th>
					<th class="px-4 py-3 text-start text-xs font-medium tracking-wide text-muted-foreground">
						{m.admin_jobs_col_lastRun()}
					</th>
					<th class="px-4 py-3 text-start text-xs font-medium tracking-wide text-muted-foreground">
						{m.admin_jobs_col_nextRun()}
					</th>
					<th class="px-4 py-3 text-end text-xs font-medium tracking-wide text-muted-foreground">
						{m.admin_jobs_col_status()}
					</th>
				</tr>
			</thead>
			<tbody>
				{#each jobs as job (job.id)}
					<!-- eslint-disable svelte/no-navigation-without-resolve -- href is pre-resolved -->
					<tr class="border-b transition-colors hover:bg-muted/50">
						<td class="px-4 py-3">
							<a href={resolve(`/admin/jobs/${job.id}`)} class="font-medium hover:underline">
								{job.id}
							</a>
						</td>
						<td class="px-4 py-3 font-mono text-xs text-muted-foreground">{job.cron}</td>
						<td class="px-4 py-3 text-muted-foreground">{formatDate(job.lastExecution)}</td>
						<td class="px-4 py-3 text-muted-foreground">{formatDate(job.nextExecution)}</td>
						<td class="px-4 py-3 text-end">
							<Badge variant={getJobStatusVariant(job.lastStatus, job.isPaused)}>
								{getJobStatusLabel(job.lastStatus, job.isPaused)}
							</Badge>
						</td>
					</tr>
				{/each}
			</tbody>
		</table>
	</div>
{/if}
