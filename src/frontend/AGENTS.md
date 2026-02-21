# Frontend Conventions (SvelteKit / Svelte 5)

## Project Structure

```
src/
├── lib/
│   ├── api/                       # client.ts, error-handling.ts, mutation.ts, v1.d.ts (GENERATED)
│   ├── auth/auth.ts               # getUser(), logout()
│   ├── components/
│   │   ├── ui/                    # shadcn (generated, customizable)
│   │   ├── auth/                  # LoginForm, RegisterDialog, ForgotPasswordForm, TurnstileWidget
│   │   ├── layout/                # Header, Sidebar, SidebarNav, UserNav, ThemeToggle, LanguageSelector
│   │   ├── profile/               # ProfileForm, AvatarDialog, AccountDetails
│   │   ├── settings/              # ChangePasswordForm, DeleteAccountDialog, ActivityLog
│   │   ├── admin/                 # UserTable, RoleCardGrid, UserManagementCard, AuditTrailCard, ...
│   │   └── common/                # StatusIndicator, WorkInProgress
│   ├── config/                    # i18n.ts (client-safe), server.ts (server-only — never export from barrel)
│   ├── state/                     # .svelte.ts files only (cooldown, shake, theme, sidebar, shortcuts)
│   ├── types/index.ts             # Type aliases from API schemas
│   └── utils/                     # cn(), permissions.ts, audit.ts, platform.ts
├── routes/
│   ├── (app)/                     # Authenticated (redirect guard)
│   │   └── admin/                 # Permission-guarded per page
│   ├── (public)/login/            # Redirect away if logged in
│   └── api/[...path]/             # Catch-all proxy to backend
├── messages/en.json, cs.json      # i18n
└── styles/                        # themes.css, tailwind.css, animations.css, base.css, utilities.css
```

## API Client

Two clients: `browserClient` (component code) and `createApiClient(fetch, url.origin)` (server load functions).

Both auto-handle 401 → refresh → retry. Server-side for initial data, client-side for mutations.

### Type Generation

**Never hand-edit `v1.d.ts`.** Regenerate: `pnpm run api:generate` (backend must be running).

Type aliases in `$lib/types/index.ts`:

```typescript
import type { components } from '$lib/api/v1';
export type User = components['schemas']['UserResponse'];
```

If the backend doesn't provide data you need — propose the endpoint, don't work around it.

## Error Handling

### Generic Errors

```typescript
import { getErrorMessage, browserClient } from '$lib/api';
const { response, error } = await browserClient.POST('/api/...', { body });
if (!response.ok) toast.error(getErrorMessage(error, m.fallback_message()));
```

`getErrorMessage()` resolves: `detail` → `title` → fallback.

### Mutations (Validation + Rate Limiting)

```typescript
import { browserClient, handleMutationError } from '$lib/api';
import { createCooldown, createFieldShakes } from '$lib/state';

const cooldown = createCooldown();
const fieldShakes = createFieldShakes();
let fieldErrors = $state<Record<string, string>>({});

const { response, error } = await browserClient.PATCH('/api/...', { body });
if (response.ok) {
	toast.success(m.success());
} else {
	handleMutationError(response, error, {
		cooldown,
		fallback: m.error(),
		onValidationError(errors) {
			fieldErrors = errors;
			fieldShakes.triggerFields(Object.keys(errors));
		}
	});
}
```

**Every rate-limited button must show countdown** during cooldown:

```svelte
<Button disabled={isLoading || cooldown.active}>
	{#if cooldown.active}{m.common_waitSeconds({ seconds: cooldown.remaining })}
	{:else if isLoading}<Loader2 class="me-2 h-4 w-4 animate-spin" />{m.submit()}
	{:else}{m.submit()}{/if}
</Button>
```

## Component Rules

### Props

Always `interface Props` + destructure from `$props()`:

```svelte
<script lang="ts">
	interface Props {
		user: User;
		onSave?: (data: FormData) => void;
		class?: string;
	}
	let { user, onSave, class: className }: Props = $props();
</script>
```

### Organization

Feature folders in `$lib/components/{feature}/` with barrel `index.ts`. Import via barrel only:

```typescript
import { ProfileForm, AvatarDialog } from '$lib/components/profile';
```

### shadcn

Add via CLI: `pnpm dlx shadcn-svelte@latest add <name>`. Check [ui.shadcn.com](https://ui.shadcn.com) before building custom UI. Convert physical CSS to logical in generated components.

## Styling

### Logical Properties Only

| Physical (never use)       | Logical (always use)      |
| -------------------------- | ------------------------- |
| `ml-*` / `mr-*`            | `ms-*` / `me-*`           |
| `pl-*` / `pr-*`            | `ps-*` / `pe-*`           |
| `left-*` / `right-*`       | `start-*` / `end-*`       |
| `text-left` / `text-right` | `text-start` / `text-end` |
| `border-l` / `border-r`    | `border-s` / `border-e`   |
| `space-x-*` on flex/grid   | `gap-*` (preferred)       |

### Responsive Design (Mobile-First)

- Base styles for 320px, then `sm:` → `md:` → `lg:` → `xl:`
- Touch targets ≥ 40px (`h-10`), primary actions ≥ 44px (`h-11`)
- `h-dvh` not `h-screen` for full-height layouts
- `min-w-0` on flex children with text, `shrink-0` on icons/badges
- **Content grids: `xl:grid-cols-2`** not `lg:` — sidebar takes ~250px
- **No `max-w-*` on page content** — cards fill their container
- Scale padding: `p-4 sm:p-6 lg:p-8` — never large flat padding
- Dialog grids: always `grid-cols-1` base with responsive breakpoint
- Min font: `text-xs` (12px) — never smaller
- Animations: always `motion-safe:` prefix

### Theming

CSS variables in `themes.css` (`:root` + `.dark`), mapped in `tailwind.css` (`@theme inline`). Use `cn()` from `$lib/utils` for class merging.

## Routing & Auth

```
hooks.server.ts → +layout.server.ts (root: getUser) → (app)/+layout.server.ts (redirect if no user)
                                                      → (public)/login (redirect if user exists)
```

Root layout fetches user **once**. Child layouts use `parent()` — never re-fetch.

### Permission Guards

1. **Admin layout**: broad gate — any admin permission
2. **Individual pages**: specific permission check → redirect to `/`
3. **Sidebar**: filters items per-permission via `hasPermission(user, item.permission)`
4. **Backend is authoritative** — frontend guards are UX only

```typescript
import { hasPermission, hasAnyPermission, Permissions } from '$lib/utils';
let canManage = $derived(hasPermission(data.user, Permissions.Users.Manage));
```

## i18n

Keys: `{domain}_{feature}_{element}` (e.g., `auth_login_title`, `profile_personalInfo_firstName`).

Add to both `en.json` and `cs.json`. Use: `import * as m from '$lib/paraglide/messages'; m.key_name()`.

`svelte-check` reports ~32 paraglide module errors — these are expected (generated at build time). Ignore them.

## State

`.svelte.ts` files in `$lib/state/` only. Never mix reactive state with pure utilities.

| File                  | Exports                                     |
| --------------------- | ------------------------------------------- |
| `cooldown.svelte.ts`  | `createCooldown()` — rate-limit countdown   |
| `shake.svelte.ts`     | `createShake()`, `createFieldShakes()`      |
| `theme.svelte.ts`     | `getTheme()`, `setTheme()`, `toggleTheme()` |
| `sidebar.svelte.ts`   | `sidebarState`, `toggleSidebar()`           |
| `shortcuts.svelte.ts` | `shortcuts` action, `getShortcutDisplay()`  |

## Security

### Response Headers (hooks.server.ts)

`X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: strict-origin-when-cross-origin`, `Permissions-Policy: camera=(), microphone=(), geolocation=()`. HSTS in production only.

### CSP (svelte.config.js)

Nonce-based `script-src`. `style-src: unsafe-inline` required for Svelte transitions. `img-src: self https: data:` for Vite-inlined assets. Cloudflare Turnstile needs `script-src` + `frame-src` for `challenges.cloudflare.com`.

### CSRF

API proxy validates `Origin` header on mutations. Same-origin + `ALLOWED_ORIGINS` env var allowed.

## Testing

Uses [vitest](https://vitest.dev/) with the SvelteKit vite config (aliases like `$lib/*` and `$app/*` resolve automatically — no separate vitest config needed).

### Conventions

- **Co-locate tests with source:** `foo.ts` → `foo.test.ts` in the same directory
- **Structure:** `describe('moduleName')` → `it('does X')` with explicit imports from `vitest`
- **Import from vitest:** `import { describe, it, expect, vi } from 'vitest'` (no implicit globals)

### Mocking

- **`vi.mock('$app/...')`** — mock SvelteKit modules (`$app/navigation`, `$app/stores`, etc.)
- **`vi.mock('$lib/...')`** — mock internal modules by path
- **`vi.fn()`** — mock individual functions; `vi.spyOn()` for partial mocks
- Reset mocks in `beforeEach` or use `vi.restoreAllMocks()` to prevent test bleed

### Running

```bash
pnpm run test              # all tests (CI mode)
pnpm run test:watch        # watch mode
pnpm run test -- -t "name" # filter by test name
```

## Don'ts

- `export let` — use `$props()`
- `$props<{...}>()` — use `interface Props` + `$props()`
- `any` — define proper interfaces
- Physical CSS (`ml-`, `mr-`, `pl-`, `pr-`, `border-l`, `border-r`)
- `space-x-*` on flex/grid — use `gap-*`
- `h-screen` — use `h-dvh`
- `lg:grid-cols-2` for content — use `xl:grid-cols-2`
- `max-w-*` on page content — cards fill container
- `null!`, `as` casts when narrowing works
- Import server config from barrel (`$lib/config`)
- Hand-edit `v1.d.ts`
- Components in `$lib/components/` root — use feature folders
- Mix `.svelte.ts` (reactive) with `.ts` (pure)
- Build what shadcn already provides
- Suppress `svelte/no-navigation-without-resolve` — use `resolve()` with `goto()`
