Create a new frontend page with routing, i18n, and navigation.

## Input

Ask the user for:
1. **Page name/route** (e.g., `orders`, `admin/reports`)
2. **Route group** — `(app)` for authenticated (default), `(public)` for unauthenticated
3. **Does it need server-side data?** (API calls on load)
4. **Components needed** (list of UI elements on the page)
5. **i18n keys needed** (page title, labels, messages)

## Steps

Follow SKILLS.md "Add a Page" and "Add a Component".

### 1. Create Route

Create `src/frontend/src/routes/(app)/{feature}/+page.svelte`:

```svelte
<script lang="ts">
    import * as m from '$lib/paraglide/messages';
</script>

<svelte:head>
    <title>{m.meta_{feature}_title()}</title>
</svelte:head>

<div class="space-y-8">
    <h1 class="text-2xl font-bold">{m.{feature}_title()}</h1>
</div>
```

### 2. Server Load (if needed)

Create `src/frontend/src/routes/(app)/{feature}/+page.server.ts`:

```typescript
import { createApiClient } from '$lib/api';
import type { PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ fetch, url }) => {
    const client = createApiClient(fetch, url.origin);
    const { data } = await client.GET('/api/v1/...');
    return { ... };
};
```

### 3. Create Components

For each component:
1. Create `src/frontend/src/lib/components/{feature}/{Name}.svelte`:
   - Use `interface Props` + `$props()`
   - Logical CSS only (`ms-*`, `me-*`, `ps-*`, `pe-*`)
   - Mobile-first responsive design
2. Create barrel `src/frontend/src/lib/components/{feature}/index.ts`:
   ```typescript
   export { default as {Name} } from './{Name}.svelte';
   ```

### 4. i18n Keys

Add to both `src/frontend/src/messages/en.json` and `src/frontend/src/messages/cs.json`:
```json
{
    "meta_{feature}_title": "Page Title",
    "{feature}_title": "Page Heading",
    "{feature}_{element}": "..."
}
```

### 5. Navigation

Update `src/frontend/src/lib/components/layout/SidebarNav.svelte`:
- Add navigation entry with appropriate icon and label
- Use `resolve('/{feature}')` for the href

### 6. Verify

```bash
cd src/frontend && npm run format && npm run lint && npm run check
```

### 7. Commit Strategy

Two atomic commits:
1. `feat({feature}): add {feature} components` — components + barrel
2. `feat({feature}): add {feature} page with routing and i18n` — route + server load + i18n + nav

## Breaking Change Awareness

- New pages are safe — additive change
- If modifying `SidebarNav.svelte`, be careful not to break existing navigation entries
- If modifying shared layout components, test that all existing pages still render correctly
- Check that new i18n keys don't conflict with existing ones
