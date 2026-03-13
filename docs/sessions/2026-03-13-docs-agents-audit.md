# Documentation, Skills & Agents Audit

**Date**: 2026-03-13
**Scope**: Comprehensive factual audit of all skills, agents, and documentation files

## Summary

Audited all 34 skill files, 12 agent definitions, and 10+ documentation files against the actual codebase state. Found and fixed factual inaccuracies (OAuth provider count, audit actions count, test count), stale references (non-existent files, old i18n structure), hardcoded locale assumptions (en/cs instead of locale-agnostic), missing content (frontend README structure diagram gaps, missing scripts), and incorrect code examples.

## Changes Made

| File | Change | Reason |
|------|--------|--------|
| `README.md` | OAuth 8->10 providers, test count 1000+->1200+, provider list updated (GitLab/Slack/Twitch, removed X), locale-agnostic i18n description | Providers and counts didn't match codebase; X/Twitter was never implemented |
| `docs/features.md` | OAuth 8->10, audit actions 25+->40, locale-agnostic i18n description | Actual counts from AuditActions.cs (40 consts) and ExternalProviders/ (10 providers) |
| `docs/architecture.md` | Updated OAuth provider list in ASCII diagram | Matched to actual provider implementations |
| `docs/troubleshooting.md` | Locale-agnostic i18n troubleshooting, removed en/cs-specific paths | Template shouldn't assume specific languages |
| `src/frontend/README.md` | Added admin/, oauth/, settings/, hooks/, schemas/ to structure diagram; fixed Props pattern example; added response field to API example; added test/watch scripts; Node.js 20+->22+; shadcn @next->@latest; added openapi-typescript to tech stack; locale-agnostic file references | Structure diagram was missing 5 directories; code example violated project convention |
| `src/frontend/project.inlang/README.md` | Updated example path from en.json to {locale}/core.json | Old monolithic file reference |
| `CLAUDE.md` | Updated pre-modification checklist to "all configured locales" | Locale-agnostic |
| `FILEMAP.md` | Updated i18n change impact entries to "all locale directories" | Locale-agnostic |
| `.claude/agents/frontend-engineer.md` | Fixed permissions example, locale-agnostic i18n | `Permissions.Feature.View` doesn't exist; used `Permissions.{Feature}.View` with real example |
| `.claude/agents/frontend-reviewer.md` | Locale-agnostic i18n checklist | Template shouldn't assume en/cs |
| `.claude/agents/fullstack-engineer.md` | Locale-agnostic i18n | Template shouldn't assume en/cs |
| `.claude/agents/product-owner.md` | Fixed stale features.md reference | Was referencing bare `features.md` without path |
| `.claude/skills/frontend-conventions/SKILL.md` | Added locales config reference | Point to settings.json as source of truth for locales |
| `.claude/skills/new-feature/SKILL.md` | Locale-agnostic i18n | Template shouldn't assume en/cs |
| `.claude/skills/new-page/SKILL.md` | Locale-agnostic i18n | Template shouldn't assume en/cs |
| `.claude/skills/new-page/assets/page.svelte.md` | Locale-agnostic i18n | Template shouldn't assume en/cs |
| `.claude/skills/review-pr/SKILL.md` | Locale-agnostic i18n review checklist | Template shouldn't assume en/cs |

## Decisions & Reasoning

### Locale-agnostic documentation

- **Choice**: Replace all "en/ and cs/" and "English + Czech" references with "all locale directories" or "all configured locales"
- **Alternatives considered**: Keep specific locale references since the template ships with en+cs
- **Reasoning**: This is a template. Users add Hungarian, Polish, Arabic, or any language. Documentation should not hardcode assumptions about which locales exist. The source of truth for configured locales is `project.inlang/settings.json`.

### OAuth provider list correction

- **Choice**: Update from 8 (with X/Twitter) to 10 (with GitLab, Slack, Twitch)
- **Alternatives considered**: Implementing X/Twitter to match the old docs
- **Reasoning**: X/Twitter was never implemented in code. GitLab, Slack, and Twitch providers were added in previous PRs but the docs were not updated. Fixed docs to match reality.

### Test count update

- **Choice**: Updated from "1000+" to "1200+"
- **Alternatives considered**: Keeping approximate "1000+"
- **Reasoning**: Actual count is 1231 (945 backend + 286 frontend). "1200+" is more accurate and still conservative.

## Follow-Up Items

- [ ] None - all issues found in the audit have been fixed in this PR
