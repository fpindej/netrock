# Claude Code Setup

How the agentic tooling in this template fits together. Read this once after cloning.

## First Run

1. Open Claude Code in the repo root and accept the workspace trust dialog.
2. The project pins three official plugins (`csharp-lsp`, `typescript-lsp`, `security-guidance`); confirm the install prompt to get LSP diagnostics and automatic security review.
3. Approve the project MCP server from `.mcp.json` (Playwright, used for browser-level verification of frontend changes).
4. Optional: copy `settings.local.json.example` to `settings.local.json` for personal permission overrides (gitignored).

The SessionStart hook checks your prerequisites (.NET SDK, pnpm, dotnet-ef, Docker) and prints what is missing.

## How the Pieces Fit

| Piece | Loaded | Purpose |
|---|---|---|
| `CLAUDE.md` | Always | Hard rules, delegation model, verification commands |
| `rules/*.md` | When matching files are touched (`paths:` frontmatter) | Implementation conventions for the main session |
| `agents/*.md` | On delegation | Engineers implement, reviewers audit read-only |
| `skills/*-conventions` | Injected into agents via `skills:` frontmatter | Full convention references (single source of truth for reviewers) |
| `skills/*` (other) | Via `/name` or automatically | Repeatable procedures (new entity, new endpoint, create PR, ...) |
| `hooks/*.mjs` | Lifecycle events (see `settings.json`) | Guardrails and automation |

## Division of Labor

- The main session is an **orchestrator**: it delegates application code in `src/` to engineer agents, runs reviewer agents in parallel afterwards, and owns all commits. Subagents never commit; they report suggested commit messages.
- Convention checklists live in the `*-conventions` skills, not in agent bodies. Change a convention in one place and every agent that preloads the skill picks it up.

## Hooks

| Hook | Event | What it does |
|---|---|---|
| `session-start.mjs` | SessionStart (startup only) | Prerequisite check: .NET, pnpm, dotnet-ef, Docker |
| `validate-bash.mjs` | PreToolUse (Bash) | Blocks destructive commands (force push, bare reset --hard, curl-pipe-sh, ...) |
| `auto-format.mjs` | PostToolUse (Write/Edit, async) | dotnet format for backend .cs, prettier for frontend files |
| `stop-quality-gate.mjs` | Stop | Blocks finishing once per dirty-file set if `src/` changes are uncommitted; reminds about feature branches |

The stop gate blocks at most once for the same set of dirty files, so it nags exactly once and never loops. Deleting its marker in your temp dir re-arms it.

## Permissions

`settings.json` allowlists the routine toolchain (dotnet, pnpm, git, gh, docker) and denies destructive operations (force pushes to master, gh auth/secret mutation, curl-pipe-sh, .env writes). The deny list intentionally overlaps with `validate-bash.mjs` - permissions are the hard gate, the hook adds clearer messages and patterns permissions cannot express. Put personal additions in `settings.local.json`, not here.
