# Migrate Commands to Skills

**Date**: 2026-03-02
**Scope**: Full rework of Claude Code agentic infrastructure - commands, skills, SKILLS.md, hard rules

## Summary

Migrated all 9 `.claude/commands/` files to `.claude/skills/*/SKILL.md` with YAML frontmatter, eliminated the 1003-line `SKILLS.md` monolith by converting all recipes into 11 additional on-demand skills, absorbed tiny recipes into AGENTS.md files, and tightened CLAUDE.md with new hard rules. Net result: -364 lines, 20 on-demand skills instead of one always-loaded blob.

## Changes Made

| File | Change | Reason |
|------|--------|--------|
| `.claude/commands/` (9 files) | Deleted | Replaced by `.claude/skills/` |
| `.claude/skills/` (20 SKILL.md files) | Created | Native Claude Code skill format with YAML frontmatter |
| `SKILLS.md` | Deleted (1003 lines) | Recipes converted to individual on-demand skills |
| `CLAUDE.md` | Added hard rules, updated file roles | No dead code, shadcn-svelte, responsiveness, touch targets, unified UX, no overflow, no em dashes |
| `AGENTS.md` | Added breaking changes section | Absorbed from SKILLS.md |
| `src/backend/AGENTS.md` | Absorbed NuGet/error/role recipes, skill pointers | Tiny recipes belong in conventions, procedures get skill links |
| `src/frontend/AGENTS.md` | Em dash cleanup | Consistency |
| `FILEMAP.md` | Em dash cleanup | Consistency |
| `README.md` | Updated SKILLS.md references | Points to `.claude/skills/` now |
| `CONTRIBUTING.md` | Updated SKILLS.md references | Points to `.claude/skills/` now |
| `docs/before-you-ship.md` | Updated skill reference | Points to `/manage-file-storage` skill |

## Decisions & Reasoning

### Skills over commands

- **Choice**: `.claude/skills/*/SKILL.md` with YAML frontmatter
- **Alternatives considered**: Keep `.claude/commands/` plain markdown
- **Reasoning**: Native Claude Code skill system supports `disable-model-invocation`, `context: fork`, `agent: Explore`, `allowed-tools`, and `argument-hint`. Only skill descriptions are always in context (~1 line each); full content loads on demand when invoked.

### Eliminate SKILLS.md entirely

- **Choice**: Convert all 29 recipes into 20 individual skills (some absorbed into AGENTS.md)
- **Alternatives considered**: Keep SKILLS.md as a smaller reference
- **Reasoning**: SKILLS.md was 1003 lines loaded as a monolith whenever Claude needed any recipe. Individual skills load only when invoked. Tiny recipes (NuGet package, error message, role addition) are better as conventions in AGENTS.md since they're 2-3 step patterns, not full procedures.

### Hard rules in CLAUDE.md, conventions in AGENTS.md, procedures in skills

- **Choice**: Three-tier separation
- **Alternatives considered**: Merge everything into fewer files
- **Reasoning**: CLAUDE.md is always loaded (constitutional rules - must be lean). AGENTS.md loads on demand (conventions for how to write code). Skills load on demand (step-by-step procedures for what to do). This minimizes token usage while keeping all knowledge accessible.

### Post-init perspective for all skills

- **Choice**: Treat all content as post-init state with `Test` as placeholder
- **Alternatives considered**: Include template-specific notes (e.g., "no EF migrations")
- **Reasoning**: Skills should reflect how the initialized project works, not template internals. Template-specific behavior is handled by the init script.

## Follow-Up Items

- [ ] Purge em dashes from source code files (separate PR - out of scope for skills migration)
