# Review: System Prompts, Skills, Commands & Agent Configuration

**Date**: 2026-02-21
**Scope**: Full audit of all Claude Code configuration files — CLAUDE.md, AGENTS.md (×3), SKILLS.md, FILEMAP.md, slash commands (×7), and .claude/ directory structure.

## Executive Summary

The prompt system is **well-crafted in content quality** — the rules are specific, actionable, and grounded in real codebase patterns. However, it suffers from three structural problems that reduce Claude Code effectiveness:

1. **Volume**: 4,775 lines of instructions across 13 files. This overwhelms the context window and dilutes instruction-following on any single rule.
2. **Duplication**: Key concepts (pre-commit checks, security principles, error handling patterns, component organization) are repeated 2–4 times across files.
3. **Flat loading model**: Claude Code loads `CLAUDE.md` always and `AGENTS.md` based on working directory — but there's no mechanism for conditional/on-demand loading of the 1,882-line backend AGENTS.md or the 1,168-line frontend AGENTS.md.

The commands are well-structured but ask too many questions upfront, reducing autonomy. There are no hooks configured, missing a key automation opportunity.

---

## File-by-File Analysis

### CLAUDE.md (61 lines) — **Good**

This is the strongest file. Concise, focused on hard rules, clear "do/don't" format.

**What works:**
- Hard rules are unambiguous and enforceable
- The Conventions Reference table is a good idea — directing the model to read specific files for specific tasks
- Pre-commit checks are concrete and copy-pasteable

**Issues:**
- The Conventions Reference table implies conditional loading ("when to read"), but Claude Code doesn't support conditional file loading. The model either loads all AGENTS.md files or none. This table creates a false promise — it suggests the model should read AGENTS.md only when doing architecture work, but the file is already in context if you're in that directory.
- The "when to read" framing should be changed to "what it contains" — since the model already has it loaded, telling it "when to read" is meaningless.

**Recommendation:** Keep as-is with minor table header change. This file is the right size and scope.

---

### AGENTS.md — Root (464 lines) — **Needs Restructuring**

This file covers too many unrelated concerns: tech stack, architecture, code quality principles, security philosophy, git discipline, issues, PRs, labels, session docs, branch hygiene, error handling, local development, infrastructure, CI/CD, Docker, deployment.

**Issues:**
1. **Mixed audience**: Some sections are for the model doing code work (architecture, error handling), others are for workflow automation (git, issues, PRs). These have different trigger contexts and shouldn't compete for attention.
2. **Section bloat**: The Issues section (lines 193–243) with sub-issues API, the PR section, Labels table — these are only needed when doing `/create-issue` or `/create-pr`. Having them always loaded wastes context.
3. **Local development section** (lines 320–401): Detailed Docker, env var precedence, developer workflows — useful but rarely needed during coding tasks.
4. **Infrastructure section** (lines 402–465): SDK versions, build config, CI/CD, Docker files, deployment — reference material that's almost never needed during active development.
5. **Duplication**: Pre-commit checks duplicated from CLAUDE.md. Security principles duplicated (also in backend AGENTS.md and frontend AGENTS.md). Error handling duplicated.

**Recommendation:** Split into focused files:
- Keep architectural overview and code quality principles in AGENTS.md (trim to ~150 lines)
- Move git/issues/PRs/labels into the commands themselves (they're only needed there)
- Move local development and infrastructure sections to `docs/development.md` (which already exists — merge or replace)
- Remove duplicated security/error handling sections (already well-covered in layer-specific AGENTS.md files)

---

### src/backend/AGENTS.md (1,882 lines) — **Needs Aggressive Trimming**

This is the largest file and the biggest context consumer. It's a comprehensive .NET reference manual — which is impressive as documentation, but counterproductive as a prompt.

**Issues:**
1. **Size**: At 1,882 lines, this alone consumes roughly 25-30% of usable context. The model will struggle to consistently follow rules buried at line 1,500.
2. **Reference vs. instruction confusion**: Much of this file is reference documentation (how the repository pattern works, how EF Core configurations are discovered, how the authorization flow works). Claude already knows these .NET patterns — the file only needs to specify project-specific deviations and conventions.
3. **Code examples are too verbose**: Full code blocks for creating entities, services, controllers, DTOs, mappers, validators — these are already in SKILLS.md as step-by-step recipes. Having them here too is redundant.
4. **Teaching vs. constraining**: Sections like "Collection Return Types", "Nullable Reference Types", "Time Abstraction" explain .NET best practices the model already knows. The file should only document project-specific rules, not reteach the language.

**What to keep (project-specific, non-obvious rules):**
- Project structure tree (trimmed — just the pattern, not every file)
- Access modifier rules table (project-specific convention)
- Entity definition rules (BaseEntity contract, private setters, protected ctor)
- AuditEvent special handling (non-BaseEntity, no FK)
- Result pattern with ErrorType convention
- DTO naming convention table
- Controller rules (ProblemFactory, never NotFound(), never anonymous objects)
- OpenAPI annotation rules
- Permission system architecture (unique to this project)
- Pagination, caching conventions

**What to remove or move to SKILLS.md:**
- Full code examples for services, repositories, configurations
- Detailed EF Core configuration explanations
- Hosting/deployment configuration details
- Rate limiting implementation details (already in SKILLS.md)
- Test infrastructure details (already in SKILLS.md)

**Target: ~600-800 lines** — less than half the current size.

---

### src/frontend/AGENTS.md (1,168 lines) — **Needs Moderate Trimming**

Better scoped than the backend AGENTS.md, but still contains significant redundancy with SKILLS.md and teaches Svelte 5 patterns Claude already knows.

**Issues:**
1. **Svelte 5 tutorial content**: Sections on Runes, reactive state, snippets, bindable props — Claude knows Svelte 5. Only document project-specific conventions (e.g., "always use `interface Props`" is a project rule; explaining how `$state` works is tutorial content).
2. **Styling section** (~150 lines): Valuable project-specific rules (logical CSS, responsive breakpoints, sidebar-aware grids), but the explanation is more verbose than needed. The rules themselves are a handful of bullet points.
3. **Route structure section** (~100 lines): Mostly explains SvelteKit's routing, which Claude knows. Project-specific bits are the auth flow diagram and permission utility usage — worth keeping.
4. **Code examples**: Many could be compressed or removed — the model knows how to write Svelte components.

**What to keep:**
- Project structure tree
- API type generation rules (project-specific workflow)
- API client architecture (the browserClient vs server client distinction)
- Error handling patterns (handleMutationError, cooldown — project-specific utilities)
- Component organization rules (feature folders, barrel exports)
- Styling rules table (logical CSS, responsive rules — but as a compact table)
- The Don'ts list (compact, scannable)

**What to remove:**
- Svelte 5 Runes tutorial (snippets, bindable props, reactive collections)
- SvelteKit routing explanation
- TypeScript patterns section (narrowing, localStorage — generic knowledge)
- Verbose code examples that duplicate SKILLS.md

**Target: ~500-600 lines.**

---

### SKILLS.md (816 lines) — **Good Structure, Minor Issues**

The best-structured file. Step-by-step recipes are exactly what makes Claude autonomous. This is the pattern other files should follow.

**Issues:**
1. **Some recipes duplicate AGENTS.md content**: The "Add an Entity" recipe repeats entity conventions from backend AGENTS.md. It should be the single source of truth for the procedure, while AGENTS.md covers only the constraints/patterns.
2. **No cross-referencing from commands**: Commands say "Follow SKILLS.md → section X" but the commands could just inline the critical steps (commands are loaded on-demand, so size matters less).
3. **Missing recipes**: No recipe for "Add an Audit Action" or "Add a Captcha-Protected Endpoint" — these are in FILEMAP.md as impact entries but not as procedures.

**Recommendation:** Keep structure. Add missing recipes. Remove content duplicated from this file in AGENTS.md files (let SKILLS.md own procedures, AGENTS.md own constraints).

---

### FILEMAP.md (218 lines) — **Excellent**

The most unique and valuable file. Change impact tables are something most projects lack.

**Issues:**
- Could benefit from a "quick lookup" section at the top (5 most common changes and their impacts)
- Some entries are overly detailed (e.g., the Captcha entries) while others that are equally complex are compact

**Recommendation:** Keep as-is. This file is the right size and provides unique value no other file replicates.

---

### Slash Commands (7 files, ~166 lines total) — **Need Autonomy Improvements**

**Current pattern:**
1. Ask user 3-6 questions upfront
2. Say "Follow SKILLS.md" and "Read AGENTS.md"
3. Report results

**Issues:**
1. **Too many upfront questions**: `/new-entity` asks for entity name, properties, feature name, enum properties — all before starting. The model should infer the feature name from the entity name, ask about enums only if relevant, and can ask about properties interactively while scaffolding.
2. **Delegation instead of instruction**: "Follow SKILLS.md → section X" means the model has to find and parse that section at runtime. Since commands are loaded on-demand (not always in context), they should include the critical steps inline — reducing the need for secondary file reads.
3. **Missing validation steps**: None of the commands specify "run pre-commit checks after each commit" — they assume the model knows this from AGENTS.md, but the command context should be self-contained.
4. **No error recovery**: Commands don't specify what to do when `dotnet build` fails or `pnpm run check` has errors.

**Recommendations:**
- Reduce upfront questions to 1-2 essential ones; infer or ask the rest as needed
- Include the core procedure steps inline (don't just reference SKILLS.md)
- Add "after completion" steps: pre-commit checks, commit, optional type regeneration
- Add error recovery: "If build fails, fix errors and retry before committing"

---

### .claude/ Directory — **Missing Key Files**

Currently only has `/commands/`. Missing:

1. **No `settings.json`**: Could configure model preferences, allowed tools, or custom permissions
2. **No hooks**: No `session-start` hook to verify the environment is ready (Docker running, dependencies installed), no pre-commit hooks to enforce checks automatically
3. **No `.claudeignore`**: Should exclude `node_modules`, `bin/`, `obj/`, build artifacts, migration files from context

---

## Cross-Cutting Issues

### 1. Duplication Map

| Concept | Appears In | Should Live In |
|---|---|---|
| Pre-commit checks | CLAUDE.md, AGENTS.md, SKILLS.md, frontend AGENTS.md | CLAUDE.md (authoritative) — others reference it |
| Security principles | AGENTS.md, backend AGENTS.md, frontend AGENTS.md | AGENTS.md (once) |
| Error handling flow | AGENTS.md, backend AGENTS.md, frontend AGENTS.md | Backend AGENTS.md (backend), Frontend AGENTS.md (frontend) |
| Component organization | Frontend AGENTS.md, SKILLS.md | Frontend AGENTS.md (conventions), SKILLS.md (procedure) |
| Entity creation | Backend AGENTS.md, SKILLS.md | SKILLS.md (procedure), backend AGENTS.md (constraints only) |
| API type generation | Frontend AGENTS.md, SKILLS.md, gen-types command | SKILLS.md (procedure), command (invocation) |

### 2. Context Budget

Rough token estimates for all prompt files:

| File | Lines | ~Tokens | Always Loaded? |
|---|---|---|---|
| CLAUDE.md | 61 | ~800 | Yes |
| AGENTS.md (root) | 464 | ~6,000 | Yes (root dir) |
| SKILLS.md | 816 | ~10,500 | No (referenced) |
| FILEMAP.md | 218 | ~2,800 | No (referenced) |
| Backend AGENTS.md | 1,882 | ~25,000 | Yes (backend dir) |
| Frontend AGENTS.md | 1,168 | ~15,000 | Yes (frontend dir) |
| **Total always-loaded** | | **~47,800** | |

When working in `src/backend/`, the model has ~32,000 tokens of instructions loaded. When the model then needs to read actual code files, this instruction overhead significantly reduces available context for the task at hand.

**Target**: Reduce always-loaded content by 40-50% through deduplication and trimming.

### 3. Autonomy Gaps

The current system is **instruction-heavy but autonomy-light**. The model has detailed rules about what to do but lacks:

- **Decision frameworks**: When should it create a sub-issue vs. handle inline? When should it ask vs. infer? The commands always ask first.
- **Error recovery patterns**: What to do when a build fails, a test breaks, a migration conflicts
- **Self-verification**: No "check your own work" instructions — e.g., after creating a controller, verify the OpenAPI spec renders correctly
- **Progressive disclosure**: Everything is presented at once instead of "read X when you need it"

---

## Recommendations — Priority Order

### P0: Reduce Context Overhead

1. **Trim backend AGENTS.md to ~700 lines**: Remove tutorial content, reduce code examples, eliminate SKILLS.md duplicates
2. **Trim frontend AGENTS.md to ~550 lines**: Remove Svelte 5 tutorials, compress styling rules, eliminate duplicates
3. **Trim root AGENTS.md to ~200 lines**: Extract git/issues/PRs into commands, move infrastructure/deployment to docs

### P1: Eliminate Duplication

4. **Single source of truth per concept**: Pre-commit checks in CLAUDE.md only, security in AGENTS.md only, procedures in SKILLS.md only
5. **Layer AGENTS.md files own constraints, SKILLS.md owns procedures**: Remove procedural content from AGENTS.md files

### P2: Improve Command Autonomy

6. **Inline critical steps in commands**: Don't delegate to SKILLS.md — include the 5-10 essential steps directly
7. **Reduce upfront questions**: Ask 1-2 essential inputs, infer the rest, ask incrementally as needed
8. **Add error recovery**: "If build fails, fix and retry" patterns
9. **Add self-verification**: "After creating X, verify Y works"

### P3: Add Missing Infrastructure

10. **Add session-start hook**: Verify Docker/dependencies on startup
11. **Add `.claudeignore`**: Exclude build artifacts, migrations, node_modules
12. **Consider AGENTS.md conditional sections**: Use clear headers like "## Reference (read when needed)" to signal what's contextual

### P4: Content Improvements

13. **Add missing SKILLS.md recipes**: Audit actions, captcha endpoints, role hierarchy changes
14. **Add decision trees to commands**: "If backend-only → skip frontend. If enum needed → ask for values."
15. **Update Conventions Reference table**: Change "When to read" to "Contains" — since loading is automatic

---

## Decisions & Reasoning

### Keep FILEMAP.md Separate

- **Choice**: FILEMAP.md remains its own file
- **Alternatives**: Merge into SKILLS.md, merge into AGENTS.md
- **Reasoning**: FILEMAP.md serves a unique purpose (impact analysis) that's orthogonal to both conventions and procedures. It's the right size and provides value no other file replicates.

### Don't Merge Backend/Frontend AGENTS.md

- **Choice**: Keep layer-specific AGENTS.md files separate
- **Alternatives**: Single AGENTS.md with all conventions
- **Reasoning**: Claude Code loads AGENTS.md based on working directory. When working in `src/frontend/`, the backend AGENTS.md shouldn't consume context. Keeping them separate means the model only loads relevant conventions. The problem is size, not separation.

### Commands Should Be More Self-Contained

- **Choice**: Inline critical steps rather than delegating to SKILLS.md
- **Alternatives**: Keep current delegation pattern
- **Reasoning**: Commands are loaded on-demand (only when invoked). This means they can afford to be larger without wasting context. Meanwhile, SKILLS.md references require the model to find and parse a section — an extra step that can go wrong. Self-contained commands = more reliable execution.

## Follow-Up Items

- [ ] Implement P0 trimming (backend AGENTS.md, frontend AGENTS.md, root AGENTS.md)
- [ ] Implement P1 deduplication pass
- [ ] Rewrite commands with inline steps and fewer upfront questions
- [ ] Add session-start hook and .claudeignore
- [ ] Add missing SKILLS.md recipes
