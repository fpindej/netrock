<script lang="ts">
	import { EmptyState } from '$lib/components/common';
	import { Badge } from '$lib/components/ui/badge';
	import * as Table from '$lib/components/ui/table';
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
	<EmptyState icon={Clock} message={m.admin_jobs_noJobs()} />
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
	<div class="hidden md:block">
		<Table.Root>
			<Table.Header>
				<Table.Row class="bg-muted/50">
					<Table.Head class="px-4 py-3 text-xs tracking-wide">
						{m.admin_jobs_col_name()}
					</Table.Head>
					<Table.Head class="px-4 py-3 text-xs tracking-wide">
						{m.admin_jobs_col_schedule()}
					</Table.Head>
					<Table.Head class="px-4 py-3 text-xs tracking-wide">
						{m.admin_jobs_col_lastRun()}
					</Table.Head>
					<Table.Head class="px-4 py-3 text-xs tracking-wide">
						{m.admin_jobs_col_nextRun()}
					</Table.Head>
					<Table.Head class="px-4 py-3 text-end text-xs tracking-wide">
						{m.admin_jobs_col_status()}
					</Table.Head>
				</Table.Row>
			</Table.Header>
			<Table.Body>
				{#each jobs as job (job.id)}
					<!-- eslint-disable svelte/no-navigation-without-resolve -- href is pre-resolved -->
					<Table.Row>
						<Table.Cell class="px-4 py-3">
							<a href={resolve(`/admin/jobs/${job.id}`)} class="font-medium hover:underline">
								{job.id}
							</a>
						</Table.Cell>
						<Table.Cell class="px-4 py-3 font-mono text-xs text-muted-foreground">
							{job.cron}
						</Table.Cell>
						<Table.Cell class="px-4 py-3 text-muted-foreground">
							{formatDate(job.lastExecution)}
						</Table.Cell>
						<Table.Cell class="px-4 py-3 text-muted-foreground">
							{formatDate(job.nextExecution)}
						</Table.Cell>
						<Table.Cell class="px-4 py-3 text-end">
							<Badge variant={getJobStatusVariant(job.lastStatus, job.isPaused)}>
								{getJobStatusLabel(job.lastStatus, job.isPaused)}
							</Badge>
						</Table.Cell>
					</Table.Row>
				{/each}
			</Table.Body>
		</Table.Root>
	</div>
{/if}
