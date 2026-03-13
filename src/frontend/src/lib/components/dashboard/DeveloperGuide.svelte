<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import { TerminalSquare } from '@lucide/svelte';
</script>

<Card.Root class="border-dashed">
	<Card.Header>
		<Card.Title class="flex items-center gap-2">
			<TerminalSquare class="size-5 text-muted-foreground" />
			Developer Quick Start
		</Card.Title>
		<Card.Description>
			This card is for you, the developer. Remove it when you're ready to ship.
		</Card.Description>
	</Card.Header>
	<Card.Content class="space-y-5 text-sm">
		<div>
			<p class="mb-1.5 font-medium">Customize this dashboard</p>
			<p class="text-muted-foreground">
				Open
				<code class="rounded bg-muted px-1.5 py-0.5 text-xs"
					>src/routes/(app)/dashboard/+page.svelte</code
				>
				and remove
				<code class="rounded bg-muted px-1.5 py-0.5 text-xs">&lt;WelcomeGuide /&gt;</code>
				and
				<code class="rounded bg-muted px-1.5 py-0.5 text-xs">&lt;DeveloperGuide /&gt;</code>. Keep
				the widgets or replace them with your own.
			</p>
		</div>

		<div>
			<p class="mb-1.5 font-medium">Add your first feature</p>
			<ol class="list-inside list-decimal space-y-1 text-muted-foreground">
				<li>
					Create an endpoint in
					<code class="rounded bg-muted px-1.5 py-0.5 text-xs">src/backend/WebApi/Controllers/</code
					>
				</li>
				<li>
					Run
					<code class="rounded bg-muted px-1.5 py-0.5 text-xs">pnpm run api:generate</code>
					to regenerate frontend types
				</li>
				<li>
					Add a page at
					<code class="rounded bg-muted px-1.5 py-0.5 text-xs"
						>src/frontend/src/routes/(app)/your-page/+page.svelte</code
					>
				</li>
				<li>
					Create components in
					<code class="rounded bg-muted px-1.5 py-0.5 text-xs"
						>src/frontend/src/lib/components/your-feature/</code
					>
				</li>
			</ol>
		</div>

		<div>
			<p class="mb-1.5 font-medium">Useful commands</p>
			<div class="grid gap-1.5 text-muted-foreground">
				<div class="flex items-baseline gap-3">
					<code class="shrink-0 rounded bg-muted px-1.5 py-0.5 text-xs">pnpm run dev</code>
					<span class="text-xs">Start dev server</span>
				</div>
				<div class="flex items-baseline gap-3">
					<code class="shrink-0 rounded bg-muted px-1.5 py-0.5 text-xs">pnpm run api:generate</code>
					<span class="text-xs">Regenerate API types from backend</span>
				</div>
				<div class="flex items-baseline gap-3">
					<code class="shrink-0 rounded bg-muted px-1.5 py-0.5 text-xs">dotnet run</code>
					<span class="text-xs">Start backend API (or use Aspire)</span>
				</div>
				<div class="flex items-baseline gap-3">
					<code class="shrink-0 rounded bg-muted px-1.5 py-0.5 text-xs"
						>dotnet ef migrations add</code
					>
					<span class="text-xs">Create a database migration</span>
				</div>
			</div>
		</div>
	</Card.Content>
</Card.Root>
