# Frontend Svelte Rules

## Svelte 5 Runes
- Runes only - never `export let` for props
- `interface Props` + `let { ... }: Props = $props()` - never `$props<{...}>()`
- Reactive state in `.svelte.ts` files in `$lib/state/` only - never mix with pure `.ts` utils

## API Client
- Never hand-edit `v1.d.ts` - run `pnpm run api:generate`
- File uploads: native `fetch()` with `FormData` - not `browserClient` (openapi-fetch breaks with multipart)
- Error handling: `getErrorMessage(error, fallback)` for simple errors, `handleMutationError()` for forms with validation

## Styling
- Logical CSS only: `ms-*`/`me-*`/`ps-*`/`pe-*`/`text-start`/`text-end` - never `ml-*`/`mr-*`/`pl-*`/`pr-*`/`text-left`/`text-right`
- `h-dvh` not `h-screen` for full-height layouts
- Content grids: `lg:grid-cols-2` (not `xl:`), page content: `max-w-7xl mx-auto`
- Buttons: default size with `w-full sm:w-auto`, right-aligned via `sm:justify-end`
- Touch targets >= 44px on primary actions, animations with `motion-safe:` prefix

## Components
- Feature folders in `$lib/components/{feature}/` with barrel `index.ts` - never root-level
- Use shadcn-svelte components - never build what shadcn already provides
- No `any` - use `unknown`, generics, or proper interfaces
- `noUncheckedIndexedAccess: true` - guard array/object index access

## i18n
- Keys: `{domain}_{feature}_{element}`, add to correct feature file in ALL locale directories
- Import: `import * as m from '$lib/paraglide/messages'`
- ~32 paraglide module errors in svelte-check are expected - ignore them
