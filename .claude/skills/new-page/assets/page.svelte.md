# Page Component Template

```svelte
<script lang="ts">
	import { PageHeader } from '$lib/components/common';
	import * as m from '$lib/paraglide/messages';
	import type { PageData } from './$types';

	let { data }: { data: PageData } = $props();
</script>

<svelte:head>
	<title>{m.meta_titleTemplate({ title: m.meta_{feature}_title() })}</title>
	<meta name="description" content={m.meta_{feature}_description()} />
</svelte:head>

<div class="space-y-6">
	<PageHeader title={m.{feature}_title()} description={m.{feature}_description()} />

	<!-- Page content -->
</div>
```

## With Permission Guard (+page.server.ts)

```typescript
import { redirect } from '@sveltejs/kit';
import { createApiClient } from '$lib/api';
import { hasPermission, Permissions } from '$lib/utils';
import type { PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ parent, fetch, url }) => {
	const { user } = await parent();

	if (!hasPermission(user, Permissions.{Feature}.View)) {
		throw redirect(303, '/');
	}

	const client = createApiClient(fetch, url.origin);
	const { data } = await client.GET('/api/v1/{feature}');

	return {
		user,
		items: data ?? []
	};
};
```

## Component Template

```svelte
<script lang="ts">
	import type { Snippet } from 'svelte';
	import { cn } from '$lib/utils';

	interface Props {
		title: string;
		description?: string;
		children: Snippet;
		class?: string;
	}

	let { title, description, children, class: className }: Props = $props();
</script>

<div class={cn('space-y-4', className)}>
	<h2 class="text-lg font-semibold">{title}</h2>
	{#if description}
		<p class="text-sm text-muted-foreground">{description}</p>
	{/if}
	{@render children()}
</div>
```

## Rules

- `interface Props` + `$props()` - never `export let` or `$props<{...}>()`
- Feature folders in `$lib/components/{feature}/` with barrel `index.ts`
- i18n keys in the correct feature file in all locale directories
- Logical CSS only: `ms-*`/`me-*`/`ps-*`/`pe-*`
- Button layout: `w-full sm:w-auto` + `flex flex-col gap-2 sm:flex-row sm:justify-end`
- Touch targets minimum 44px on interactive elements
- Responsive: base 320px, then `sm:` / `md:` / `lg:` / `xl:`
