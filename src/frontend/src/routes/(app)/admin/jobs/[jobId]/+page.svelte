<script lang="ts">
	import { Separator } from '$lib/components/ui/separator';
	import { JobInfoCard, JobActionsCard, JobExecutionHistory } from '$lib/components/admin';
	import { hasPermission, Permissions } from '$lib/utils';
	import { setDynamicLabel, clearDynamicLabel } from '$lib/state/breadcrumb.svelte';
	import * as m from '$lib/paraglide/messages';
	import type { PageData } from './$types';

	let { data }: { data: PageData } = $props();

	let canManageJobs = $derived(hasPermission(data.user, Permissions.Jobs.Manage));

	$effect(() => {
		setDynamicLabel(data.job?.id ?? '');
		return () => clearDynamicLabel();
	});
</script>

<svelte:head>
	<title>{m.meta_titleTemplate({ title: data.job?.id ?? m.meta_adminJobDetail_title() })}</title>
	<meta name="description" content={m.meta_adminJobDetail_description()} />
</svelte:head>

<div class="space-y-6">
	<div class="space-y-1">
		<p class="font-mono text-sm text-muted-foreground">{data.job?.cron}</p>
	</div>
	<Separator />

	{#if data.job}
		<div class="grid gap-6 lg:grid-cols-2">
			<JobInfoCard job={data.job} />
			{#if canManageJobs}
				<JobActionsCard jobId={data.job.id ?? ''} isPaused={data.job.isPaused ?? false} />
			{/if}
		</div>

		<JobExecutionHistory executions={data.job.executionHistory ?? []} />
	{/if}
</div>
