# Code Coverage Reporting + CI Pipeline Review

**Date**: 2026-02-15
**Scope**: Add Coverlet-based code coverage collection to the CI pipeline with GitHub Job Summary reporting; review existing CI pipeline quality.

## Summary

Added `coverlet.collector` to all 4 test projects to enable code coverage data collection during CI runs. Updated the `backend-build` job in `ci.yml` to collect XPlat Code Coverage, generate a Markdown summary via ReportGenerator, and publish it to the GitHub Actions Job Summary. Also reviewed both `ci.yml` and `docker.yml` pipelines for quality and atomicity — no structural issues found.

## Changes Made

| File | Change | Reason |
|------|--------|--------|
| `src/backend/Directory.Packages.props` | Added `coverlet.collector` 6.0.4 to Test item group | Central package version management |
| `src/backend/tests/MyProject.Unit.Tests/MyProject.Unit.Tests.csproj` | Added `coverlet.collector` PackageReference | Enable coverage collection |
| `src/backend/tests/MyProject.Component.Tests/MyProject.Component.Tests.csproj` | Added `coverlet.collector` PackageReference | Enable coverage collection |
| `src/backend/tests/MyProject.Api.Tests/MyProject.Api.Tests.csproj` | Added `coverlet.collector` PackageReference | Enable coverage collection |
| `src/backend/tests/MyProject.Architecture.Tests/MyProject.Architecture.Tests.csproj` | Added `coverlet.collector` PackageReference | Enable coverage collection |
| `.github/workflows/ci.yml` | Replaced single Test step with coverage collection + report pipeline | Coverage visibility in CI |

## Decisions & Reasoning

### `continue-on-error` + final fail step pattern

- **Choice**: Use `continue-on-error: true` on the Test step with a final `Fail if tests failed` step
- **Alternatives considered**: Running coverage as a separate job; using `if: always()` on report steps without `continue-on-error`
- **Reasoning**: Coverlet generates coverage data even when tests fail. Without `continue-on-error`, a test failure skips all subsequent steps (including report generation). The pattern ensures coverage reports are always generated while the final step re-asserts the test outcome so CI still fails properly.

### No thresholds, badges, or PR comments

- **Choice**: Only publish to GitHub Job Summary
- **Alternatives considered**: Coverage thresholds that fail the build; badge generation; PR comment bots
- **Reasoning**: Keep the initial implementation minimal. The Job Summary is visible directly on the Actions run page. Thresholds and badges can be layered on later once baseline coverage is established.

### ReportGenerator GitHub Action for Markdown

- **Choice**: Use `danielpalme/ReportGenerator-GitHub-Action@v5` with `MarkdownSummaryGithub` report type
- **Alternatives considered**: Custom scripts to parse Cobertura XML; other coverage report tools
- **Reasoning**: ReportGenerator is the de facto standard for .NET coverage reports. The `MarkdownSummaryGithub` type produces a table specifically formatted for `$GITHUB_STEP_SUMMARY`.

## CI Pipeline Review

Both `ci.yml` and `docker.yml` were reviewed for quality, atomicity, and correctness:

- **Path filtering**: `ci.yml` uses `dorny/paths-filter` inside the workflow (so `ci-passed` always exists for branch protection). `docker.yml` uses workflow-level `paths:` triggers. Good design choice — different strategies for required vs non-required checks.
- **Concurrency**: Both cancel in-progress PR runs but not master pushes. Correct behavior.
- **Caching**: NuGet cache keyed on `.csproj` + `Directory.Packages.props`. npm cache on `package-lock.json`. Both correct.
- **Gatekeeper**: `ci-passed` checks for failure/cancelled across all jobs. Solid pattern.
- **Docker**: BuildX with GHA cache, tests excluded via `.dockerignore`. Clean.

No structural changes were needed to either pipeline.

## Follow-Up Items

- [ ] Establish baseline coverage numbers and consider adding minimum thresholds
- [ ] Consider adding coverage trend tracking across PRs
- [ ] Optionally add badge generation for README display
