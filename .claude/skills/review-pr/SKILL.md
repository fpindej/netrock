---
context: fork
agent: Explore
allowed-tools: Read, Glob, Grep, Bash
argument-hint: "[PR number or URL]"
---

Review a pull request for production-readiness before merge.

Argument: PR number or URL. If omitted, review the current branch's open PR.

## Steps

1. Resolve the PR: `gh pr view {number} --json number,title,headRefName,body`
2. Get the full diff: `gh pr diff {number}`
3. Read every changed file in full (not just the diff) to understand surrounding context
4. Read the relevant AGENTS.md (`src/frontend/AGENTS.md` or `src/backend/AGENTS.md`) for conventions

## Review Checklist

- **Correctness**: Does the code do what the PR description says? Edge cases handled?
- **Type safety**: TypeScript types align, no `any`, no unsafe casts
- **Security**: No information leakage, no auth bypasses, inputs validated
- **i18n**: If i18n keys added - present in both `en.json` AND `cs.json`, Czech translations correct
- **Conventions**: Matches project patterns (Props, logical CSS, Result pattern, etc.)
- **Completeness**: Are new flags/props consumed where needed? No dead code introduced?
- **Tests**: If behavior changed, are tests added or updated?
- **No em dashes**: Flag any `—` (em dash) usage - it's an AI tell and a project rule violation. Use `-` or rewrite.
- **No emojis**: Flag any emoji usage in code, UI text, or comments - project rule violation.
- **Dockerfile**: If a new `.csproj` project was added that WebApi references, verify it has a COPY line in the Dockerfile restore layer.
- **Production-grade**: This is production code. Every pattern, fix, and decision must be production-quality.

## Output Format

Report findings as:

- **PASS** - what looks good (brief, no padding)
- **FAIL** - issues that MUST be fixed before merge (with file path and line)
- **WARN** - suggestions, not blockers

End with a verdict: `APPROVE`, `REQUEST CHANGES` (has FAIL items), or `APPROVE WITH SUGGESTIONS` (only WARN items).

## Rules

- Research only - do NOT modify any files
- Read actual source files, not just the diff - context matters
- Be thorough but not pedantic - flag real issues, not style nitpicks already handled by linters
- If the PR touches both frontend and backend, check cross-stack consistency (types, API contract)
