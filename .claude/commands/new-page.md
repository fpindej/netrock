Create a new frontend page with routing, i18n, and navigation.

Infer route, route group, components needed, and data requirements from context. Default to `(app)` (authenticated). Ask only if genuinely ambiguous.

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

**Verify and commit:**

10. `cd src/frontend && pnpm run format && pnpm run lint && pnpm run check` — fix errors, loop until green
11. Commit: `feat({feature}): add {feature} page`

Paraglide module errors (~32) are expected at check time — ignore those. Fix everything else.
