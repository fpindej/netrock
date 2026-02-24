<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import { Badge } from '$lib/components/ui/badge';
	import { Clock, Calendar, History, Code } from '@lucide/svelte';
	import { InfoItem } from '$lib/components/profile';
	import * as m from '$lib/paraglide/messages';
	import type { JobDetail } from '$lib/types';
	import { formatJobDate, getJobStatusLabel } from '$lib/utils/jobs';

	interface Props {
		job: JobDetail;
	}

	let { job }: Props = $props();

	function formatDate(date: string | null | undefined): string {
		return formatJobDate(date, m.admin_jobs_never());
	}
</script>

<Card.Root>
	<Card.Header>
		<Card.Title>{m.admin_jobDetail_info()}</Card.Title>
		<Card.Description>{m.admin_jobDetail_infoDescription()}</Card.Description>
	</Card.Header>
	<Card.Content class="grid gap-4 sm:grid-cols-2">
		<InfoItem icon={Code} label={m.admin_jobDetail_cronExpression()}>
			<code class="text-xs">{job.cron}</code>
		</InfoItem>

		<InfoItem icon={Clock} label={m.admin_jobDetail_schedule()}>
			{#if job.isPaused}
				<Badge variant="outline">{m.admin_jobs_status_paused()}</Badge>
			{:else}
				<Badge variant="default">{getJobStatusLabel(job.lastStatus, job.isPaused)}</Badge>
			{/if}
		</InfoItem>

		<InfoItem icon={History} label={m.admin_jobDetail_lastExecution()}>
			{formatDate(job.lastExecution)}
		</InfoItem>

		<InfoItem icon={Calendar} label={m.admin_jobDetail_nextExecution()}>
			{#if job.isPaused}
				<span class="text-muted-foreground">{m.admin_jobs_status_paused()}</span>
			{:else}
				{formatDate(job.nextExecution)}
			{/if}
		</InfoItem>
	</Card.Content>
</Card.Root>
