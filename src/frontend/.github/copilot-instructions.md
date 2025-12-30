# GitHub Copilot Instructions

You are an expert SvelteKit developer working on a production-grade application. Maintain S-Tier architecture: scalable, maintainable, and type-safe.

---

## Tech Stack

| Layer     | Technology                     | Notes                                     |
| --------- | ------------------------------ | ----------------------------------------- |
| Framework | SvelteKit + **Svelte 5 Runes** | `$state`, `$props`, `$effect`, `$derived` |
| Language  | TypeScript (Strict)            | No `any`, define interfaces               |
| Styling   | Tailwind CSS 4                 | CSS variables in `layout.css`             |
| UI        | shadcn-svelte (`bits-ui`)      | Headless, accessible components           |
| i18n      | `paraglide-js`                 | Type-safe `m.domain_feature_key()`        |
| API       | `openapi-fetch`                | Type-safe client from OpenAPI spec        |
| Backend   | ASP.NET Core                   | ProblemDetails error format               |

---

## Project Structure

```
src/lib/
├── api/                    # API client & error handling
│   ├── client.ts           # createApiClient(), browserClient
│   ├── error-handling.ts   # isValidationProblemDetails(), mapFieldErrors()
│   └── v1.d.ts             # Generated OpenAPI types
│
├── auth/                   # Authentication feature
│   └── auth.ts             # getUser(), logout()
│
├── config/                 # Configuration
│   ├── i18n.ts             # LANGUAGE_METADATA (client-safe)
│   ├── index.ts            # Client-safe exports only
│   └── server.ts           # SERVER_CONFIG (import directly, not from barrel)
│
├── state/                  # Reactive state (.svelte.ts files)
│   ├── shake.svelte.ts     # createShake(), createFieldShakes()
│   ├── shortcuts.svelte.ts # Keyboard shortcuts
│   └── theme.svelte.ts     # getTheme(), setTheme(), toggleTheme()
│
├── types/                  # Type aliases
│   └── index.ts            # User, etc.
│
├── utils/                  # Pure utility functions
│   ├── platform.ts         # IS_MAC, IS_WINDOWS
│   └── ui.ts               # cn() for class merging
│
└── components/
    ├── ui/                 # shadcn components (presentational only)
    ├── auth/               # LoginForm, RegisterDialog
    ├── layout/             # Header, Sidebar, UserNav
    ├── profile/            # ProfileForm, AvatarDialog
    └── common/             # Shared components
```

### Import Rules

```typescript
// ✅ Use barrel exports
import { Header, Sidebar } from '$lib/components/layout';
import { createShake } from '$lib/state';
import { isValidationProblemDetails, browserClient } from '$lib/api';
import { cn } from '$lib/utils';

// ❌ Never import directly from files
import Header from '$lib/components/layout/Header.svelte';

// ⚠️ Server config: import directly (not from barrel)
import { SERVER_CONFIG } from '$lib/config/server';
```

---

## Svelte 5 Patterns

### Component Props

```svelte
<script lang="ts">
	interface Props {
		user: User;
		onSave?: (data: FormData) => void;
		class?: string; // Allow className passthrough
	}

	let { user, onSave, class: className }: Props = $props();
</script>
```

### Reactive State

```svelte
<script lang="ts">
	// Local state
	let count = $state(0);
	let items = $state<string[]>([]);

	// Derived values
	let doubled = $derived(count * 2);
	let hasItems = $derived(items.length > 0);

	// Effects (side effects on state change)
	$effect(() => {
		console.log('Count changed:', count);
	});
</script>
```

### Bindable Props

```svelte
<script lang="ts">
	interface Props {
		open: boolean; // Two-way binding
	}

	let { open = $bindable() }: Props = $props();
</script>

<!-- Usage -->
<Dialog bind:open={isOpen} />
```

### Snippets (replacing slots)

```svelte
<!-- Child (Card.svelte) -->
<script lang="ts">
	import type { Snippet } from 'svelte';

	interface Props {
		header?: Snippet;
		content?: Snippet;
	}

	let { header, content }: Props = $props();
</script>

<!-- Parent -->
<Card>
	{#snippet header()}
		<h2>Title</h2>
	{/snippet}
	{#snippet content()}
		<p>Body</p>
	{/snippet}
</Card>

<div class="card">
	{#if header}{@render header()}{/if}
	{#if content}{@render content()}{/if}
</div>
```

---

## API & Error Handling

### Making API Calls

```typescript
import { browserClient } from '$lib/api';

const { data, response, error } = await browserClient.GET('/api/users/me');

if (response.ok && data) {
	// Success
} else if (error) {
	// Handle error
}
```

### Handling Validation Errors (ASP.NET Core)

```typescript
import { isValidationProblemDetails, mapFieldErrors } from '$lib/api';

if (isValidationProblemDetails(apiError)) {
	// Maps { PhoneNumber: ["Invalid"] } → { phoneNumber: "Invalid" }
	fieldErrors = mapFieldErrors(apiError.errors);
	fieldShakes.triggerFields(Object.keys(fieldErrors));
} else {
	toast.error(apiError?.detail || 'An error occurred');
}
```

### Field-Level Shake Animation

```svelte
<script lang="ts">
	import { createFieldShakes } from '$lib/state';

	const fieldShakes = createFieldShakes();

	async function handleSubmit() {
		// On validation error:
		fieldShakes.trigger('phoneNumber');
		// Or multiple:
		fieldShakes.triggerFields(['email', 'password']);
	}
</script>

<Input class={fieldShakes.class('phoneNumber')} />
```

---

## Styling (Tailwind 4)

### RTL Support - Use Logical Properties

```html
<!-- ✅ Correct -->
<div class="ms-4 me-2 ps-3 pe-1 text-start">
	<!-- ❌ Wrong (breaks RTL) -->
	<div class="mr-2 ml-4 pr-1 pl-3 text-left"></div>
</div>
```

| Physical    | Logical                |
| ----------- | ---------------------- |
| `ml-*`      | `ms-*` (margin-start)  |
| `mr-*`      | `me-*` (margin-end)    |
| `pl-*`      | `ps-*` (padding-start) |
| `pr-*`      | `pe-*` (padding-end)   |
| `left-*`    | `start-*`              |
| `right-*`   | `end-*`                |
| `text-left` | `text-start`           |

### Class Merging

```svelte
<script lang="ts">
	import { cn } from '$lib/utils';

	interface Props {
		class?: string;
		variant?: 'default' | 'destructive';
	}

	let { class: className, variant = 'default' }: Props = $props();
</script>

<button class={cn('rounded px-4 py-2', variant === 'destructive' && 'bg-red-500', className)}>
	<slot />
</button>
```

---

## Internationalization (i18n)

### Message Keys Convention

```
{domain}_{feature}_{element}
```

Examples:

- `auth_login_title`
- `profile_personalInfo_firstName`
- `nav_dashboard`

### Usage

```svelte
<script lang="ts">
	import * as m from '$lib/paraglide/messages';
</script>

<h1>{m.auth_login_title()}</h1>
<Label>{m.profile_personalInfo_firstName()}</Label>

<svelte:head>
	<title>{m.meta_profile_title()}</title>
	<meta name="description" content={m.meta_profile_description()} />
</svelte:head>
```

### Adding New Keys

Edit `src/messages/en.json` and `src/messages/cs.json`:

```json
{
	"profile_newFeature_label": "New Feature"
}
```

---

## Routes Structure

```
src/routes/
├── (app)/              # Authenticated routes
│   ├── +layout.svelte  # App shell (sidebar, header)
│   ├── +page.svelte    # Dashboard
│   ├── profile/
│   └── settings/
│
├── (public)/           # Public routes
│   └── login/
│
├── api/                # API proxy routes
│   └── [...path]/      # Proxy to backend
│
├── +layout.svelte      # Root layout
└── +error.svelte       # Error page
```

---

## Quality Checklist

Before marking any task complete, run ALL of these:

```bash
npm run format   # Prettier
npm run lint     # ESLint
npm run check    # Svelte type check
npm run build    # Production build
```

If any fail, fix the issues before proceeding.

---

## Commit Convention

Use [Conventional Commits](https://www.conventionalcommits.org/):

```
feat: add phone input to registration form
fix: display specific validation errors in RegisterDialog
refactor: move shake utilities to state folder
chore: update dependencies
docs: improve README
```

Keep commits atomic and focused on a single change.

---

## Common Patterns

### Form with Validation

```svelte
<script lang="ts">
	import { createFieldShakes } from '$lib/state';
	import { isValidationProblemDetails, mapFieldErrors, browserClient } from '$lib/api';
	import { toast } from '$lib/components/ui/sonner';
	import * as m from '$lib/paraglide/messages';

	let isLoading = $state(false);
	let fieldErrors = $state<Record<string, string>>({});
	const fieldShakes = createFieldShakes();

	async function handleSubmit(e: Event) {
		e.preventDefault();
		isLoading = true;
		fieldErrors = {};

		const { response, error: apiError } = await browserClient.PATCH('/api/users/me', {
			body: { firstName, lastName }
		});

		if (response.ok) {
			toast.success(m.profile_updateSuccess());
		} else if (isValidationProblemDetails(apiError)) {
			fieldErrors = mapFieldErrors(apiError.errors);
			fieldShakes.triggerFields(Object.keys(fieldErrors));
		} else {
			toast.error(m.profile_updateError());
		}

		isLoading = false;
	}
</script>

<form onsubmit={handleSubmit}>
	<Input
		bind:value={firstName}
		class={fieldShakes.class('firstName')}
		aria-invalid={!!fieldErrors.firstName}
	/>
	{#if fieldErrors.firstName}
		<p class="text-xs text-destructive">{fieldErrors.firstName}</p>
	{/if}
</form>
```

### Dialog Component

```svelte
<script lang="ts">
	import * as Dialog from '$lib/components/ui/dialog';

	let { open = $bindable(false) }: { open?: boolean } = $props();
</script>

<Dialog.Root bind:open>
	<Dialog.Content>
		<Dialog.Header>
			<Dialog.Title>Title</Dialog.Title>
			<Dialog.Description>Description</Dialog.Description>
		</Dialog.Header>
		<!-- Content -->
		<Dialog.Footer>
			<Button>Save</Button>
		</Dialog.Footer>
	</Dialog.Content>
</Dialog.Root>
```

---

## Don'ts

- ❌ Don't use `export let` (use `$props()`)
- ❌ Don't use `any` type
- ❌ Don't use physical CSS properties (`ml-`, `mr-`, `left-`, `right-`)
- ❌ Don't import server config from the barrel (`$lib/config`)
- ❌ Don't leave components in `src/lib/components/` root
- ❌ Don't skip the quality checklist
- ❌ Don't mix reactive state (`.svelte.ts`) with pure utils
