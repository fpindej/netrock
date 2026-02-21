Create a new frontend page with routing, i18n, and navigation.

Ask the user: **What page?** (e.g., "order history page at /orders"). Default to `(app)` route group (authenticated) unless told otherwise.

## Steps

**Components (if needed):**

1. Create feature folder: `src/frontend/src/lib/components/{feature}/`
2. Create components with `interface Props` + `$props()`
3. Create barrel `index.ts` exporting all components

**Page:**

4. Create route directory: `src/frontend/src/routes/(app)/{feature}/`
   - Or `(public)/{feature}/` for unauthenticated pages
5. Create `+page.svelte` with `<svelte:head>` using i18n title
6. If server data needed: create `+page.server.ts` using `createApiClient(fetch, url.origin)`
7. If permission-guarded: add check in `+page.server.ts`:
   ```typescript
   if (!hasPermission(user, Permissions.Feature.View)) throw redirect(303, '/');
   ```

**Integration:**

8. Add i18n keys to both `en.json` and `cs.json`
9. Add navigation entry in `SidebarNav.svelte` (with `permission` field if guarded)

**Verify:**

10. `cd src/frontend && pnpm run format && pnpm run lint && pnpm run check` — fix any errors
11. Commit: `feat({feature}): add {feature} page`

**Error recovery:** If `pnpm run check` fails, fix type errors before committing. Paraglide module errors (~32) are expected — ignore those.
