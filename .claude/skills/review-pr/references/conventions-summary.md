# Conventions Quick Reference (for PR Review)

## Backend Hard Rules

- `Result`/`Result<T>` for all fallible operations - never throw for business logic
- `TimeProvider` injected - never `DateTime.UtcNow`
- C# 13 `extension(T)` syntax for new extension methods
- Never `null!` - fix the design instead
- `ProblemDetails` (RFC 9457) for all error responses
- `internal` on all Infrastructure service implementations
- `/// <summary>` XML docs on all public and internal API surface
- `System.Text.Json` only - never Newtonsoft
- NuGet versions in `Directory.Packages.props` only

## Frontend Hard Rules

- Never hand-edit `v1.d.ts` - run `pnpm run api:generate`
- Svelte 5 Runes only - never `export let`
- `interface Props` + `$props()` - never `$props<{...}>()`
- Logical CSS only: `ms-*`/`me-*`/`ps-*`/`pe-*` - never `ml-*`/`mr-*`/`pl-*`/`pr-*`
- No `any` - define proper interfaces
- Feature folders in `$lib/components/{feature}/` with barrel `index.ts`
- shadcn components - never build what shadcn provides
- Touch targets minimum 44px on all interactive elements
- No overflow in dialogs - content fits viewport
- Button layout: `w-full sm:w-auto` with `flex flex-col gap-2 sm:flex-row sm:justify-end`

## Cross-Cutting Rules

- No dead code
- No em dashes (U+2014) anywhere
- No emojis anywhere
- Atomic commits: `type(scope): imperative description`
- Security first - deny by default
- PII compliance: `users.view_pii` permission, server-side masking

## DTO Naming

| Layer | Pattern |
|---|---|
| WebApi Request | `{Operation}Request` |
| WebApi Response | `{Entity}Response` |
| Application Input | `{Operation}Input` |
| Application Output | `{Entity}Output` |

## Error Flow

Backend `ErrorMessages.*` -> `Result.Failure()` -> `ProblemFactory.Create()` -> `ProblemDetails.detail` -> Frontend `getErrorMessage()`

## Dockerfile

When a new `.csproj` project is added that WebApi references, it needs a COPY line in the Dockerfile restore layer.
