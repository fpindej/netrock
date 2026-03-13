<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import { TerminalSquare, ChevronDown } from '@lucide/svelte';
	import * as m from '$lib/paraglide/messages';
</script>

<Card.Root class="border-dashed">
	<details class="group">
		<summary
			class="flex cursor-pointer list-none items-center gap-3 p-6 [&::-webkit-details-marker]:hidden"
		>
			<TerminalSquare class="size-5 shrink-0 text-muted-foreground" />
			<div class="min-w-0 flex-1">
				<p class="text-base leading-none font-semibold tracking-tight">
					{m.dashboard_dev_title()}
				</p>
				<p class="mt-1.5 text-sm text-muted-foreground">{m.dashboard_dev_description()}</p>
			</div>
			<ChevronDown
				class="size-4 shrink-0 text-muted-foreground transition-transform group-open:rotate-180"
			/>
		</summary>
		<div class="space-y-5 px-6 pb-6 text-sm">
			<div>
				<p class="mb-1.5 font-medium">{m.dashboard_dev_customize()}</p>
				<p class="text-muted-foreground">
					{m.dashboard_dev_customizeDesc()}
				</p>
				<ul class="mt-1.5 list-inside list-disc space-y-0.5 text-muted-foreground">
					<li>
						{m.dashboard_dev_customizeRemove()}
						<code class="rounded bg-muted px-1.5 py-0.5 text-xs">&lt;WelcomeGuide /&gt;</code>,
						<code class="rounded bg-muted px-1.5 py-0.5 text-xs">&lt;DeveloperGuide /&gt;</code>
					</li>
					<li>
						{m.dashboard_dev_customizeFile()}
						<code class="rounded bg-muted px-1.5 py-0.5 text-xs"
							>src/routes/(app)/dashboard/+page.svelte</code
						>
					</li>
				</ul>
			</div>

			<div>
				<p class="mb-1.5 font-medium">{m.dashboard_dev_addFeature()}</p>
				<ol class="list-inside list-decimal space-y-1 text-muted-foreground">
					<li>
						{m.dashboard_dev_stepEndpoint()}
						<code class="rounded bg-muted px-1.5 py-0.5 text-xs"
							>src/backend/WebApi/Controllers/</code
						>
					</li>
					<li>
						{m.dashboard_dev_stepGenerate()}
						<code class="rounded bg-muted px-1.5 py-0.5 text-xs">pnpm run api:generate</code>
					</li>
					<li>
						{m.dashboard_dev_stepPage()}
						<code class="rounded bg-muted px-1.5 py-0.5 text-xs"
							>src/frontend/src/routes/(app)/your-page/</code
						>
					</li>
					<li>
						{m.dashboard_dev_stepComponents()}
						<code class="rounded bg-muted px-1.5 py-0.5 text-xs"
							>src/frontend/src/lib/components/your-feature/</code
						>
					</li>
				</ol>
			</div>

			<div>
				<p class="mb-1.5 font-medium">{m.dashboard_dev_commands()}</p>
				<div class="grid gap-1.5 text-muted-foreground">
					<div class="flex items-baseline gap-3">
						<code class="shrink-0 rounded bg-muted px-1.5 py-0.5 text-xs">pnpm run dev</code>
						<span class="text-xs">{m.dashboard_dev_cmdDev()}</span>
					</div>
					<div class="flex items-baseline gap-3">
						<code class="shrink-0 rounded bg-muted px-1.5 py-0.5 text-xs"
							>pnpm run api:generate</code
						>
						<span class="text-xs">{m.dashboard_dev_cmdGenerate()}</span>
					</div>
					<div class="flex items-baseline gap-3">
						<code class="shrink-0 rounded bg-muted px-1.5 py-0.5 text-xs">dotnet run</code>
						<span class="text-xs">{m.dashboard_dev_cmdDotnet()}</span>
					</div>
					<div class="flex items-baseline gap-3">
						<code class="shrink-0 rounded bg-muted px-1.5 py-0.5 text-xs"
							>dotnet ef migrations add</code
						>
						<span class="text-xs">{m.dashboard_dev_cmdMigration()}</span>
					</div>
				</div>
			</div>

			<div>
				<p class="mb-1.5 font-medium">{m.dashboard_dev_cleanup()}</p>
				<ul class="list-inside list-disc space-y-0.5 text-muted-foreground">
					<li>
						{m.dashboard_dev_cleanupComponents()}
						<code class="rounded bg-muted px-1.5 py-0.5 text-xs">src/lib/components/dashboard/</code
						>
					</li>
					<li>
						{m.dashboard_dev_cleanupKeys()}
						<code class="rounded bg-muted px-1.5 py-0.5 text-xs">dashboard_guide_*</code>,
						<code class="rounded bg-muted px-1.5 py-0.5 text-xs">dashboard_dev_*</code>
					</li>
					<li>
						{m.dashboard_dev_cleanupBarrel()}
						<code class="rounded bg-muted px-1.5 py-0.5 text-xs"
							>src/lib/components/dashboard/index.ts</code
						>
					</li>
				</ul>
			</div>
		</div>
	</details>
</Card.Root>
