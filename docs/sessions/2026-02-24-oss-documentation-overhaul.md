# OSS Documentation Overhaul

**Date**: 2026-02-24
**Scope**: Add standard open-source governance files and fix existing documentation gaps

## Summary

Added all standard OSS community health files that GitHub's Community Standards checklist expects: security policy, code of conduct, issue templates, PR template, and funding config. Fixed a missing `pnpm run test` step in CONTRIBUTING.md and updated the LICENSE copyright year.

## Changes Made

| File | Change | Reason |
|------|--------|--------|
| `SECURITY.md` | Created vulnerability disclosure policy | GitHub Security tab integration, responsible disclosure guidance |
| `CODE_OF_CONDUCT.md` | Added Contributor Covenant v2.1 | Standard OSS governance, GitHub Community Standards |
| `.github/ISSUE_TEMPLATE/bug_report.yml` | Created YAML-form bug report template | Structured bug reports with auto-labeling |
| `.github/ISSUE_TEMPLATE/feature_request.yml` | Created YAML-form feature request template | Includes "Why It Belongs in the Template" gate |
| `.github/ISSUE_TEMPLATE/config.yml` | Created template chooser config | Enables blank issues, adds Discord contact link |
| `.github/PULL_REQUEST_TEMPLATE.md` | Created PR template with checklist | Standardize PR descriptions, verification reminders |
| `.github/FUNDING.yml` | Created funding config | Buy Me a Coffee sponsor button on GitHub |
| `CONTRIBUTING.md` | Added `pnpm run test`, linked SECURITY.md and CoC | Fix verification commands to match CLAUDE.md and CI |
| `LICENSE` | Updated copyright year to 2025-2026 | Reflect current year |
| `docs/security.md` | Added vulnerability reporting link | Cross-link to SECURITY.md from architecture docs |

## Decisions & Reasoning

### Contributor Covenant v2.1 for Code of Conduct

- **Choice**: Standard Contributor Covenant rather than a custom CoC
- **Alternatives considered**: Custom code of conduct, GitHub's default
- **Reasoning**: Industry standard, recognized by GitHub's Community Standards, well-understood by contributors. No reason to deviate.

### YAML-form issue templates over Markdown templates

- **Choice**: `.yml` form-based templates instead of `.md` templates
- **Alternatives considered**: Markdown issue templates
- **Reasoning**: YAML forms enforce structure (required fields, proper sections), reduce low-quality bug reports, and auto-apply labels. The "Why It Belongs in the Template" required field on feature requests is a particularly effective gate for template projects.

### GitHub Private Vulnerability Reporting as primary channel

- **Choice**: GHPVR preferred, email as fallback
- **Alternatives considered**: Email-only, dedicated security contact form
- **Reasoning**: GHPVR keeps the conversation within GitHub's infrastructure, provides structured advisory workflow, and is the modern standard. Email fallback covers contributors who can't use GHPVR.

### Scope limitation for initialized projects

- **Choice**: Explicitly state that post-init security is the user's responsibility
- **Alternatives considered**: Offering ongoing security support for derived projects
- **Reasoning**: NETrock is a template — once initialized and customized, the maintainer can't realistically guarantee security of every fork. Clear scope prevents mismatched expectations.

## Follow-Up Items

- [ ] Consider adding a supported versions table to SECURITY.md as the project matures
- [ ] Add SECURITY.md and CODE_OF_CONDUCT.md links to README.md in a future README update
- [ ] Consider `CODEOWNERS` if the maintainer count grows beyond one
