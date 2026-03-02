---
description: Reviews a Dependabot PR and evaluates whether the dependency update is safe to merge. Use when a Dependabot PR is opened, when evaluating dependency updates, or when asked to check if a version bump is safe.
context: fork
agent: Explore
allowed-tools: Read, Glob, Grep, Bash, WebFetch
argument-hint: "[PR number or URL]"
---

Reviews a Dependabot PR and evaluates whether it is safe to merge.

Argument: PR number or URL.

## Steps

1. Fetch PR metadata: `gh pr view {number} --json number,title,headRefName,body,labels`
2. Get the diff: `gh pr diff {number}`
3. Identify the package, old version, new version, and version bump type (patch / minor / major)

## Determine scope

| Signal | How to check |
|---|---|
| Backend (.NET) | Diff touches `Directory.Packages.props` |
| Frontend (pnpm) | Diff touches `package.json` / `pnpm-lock.yaml` |
| Dev-only dependency | Package appears only in test projects, build tooling, or `devDependencies` |
| Production dependency | Package is imported/used in shipped code |

## Evaluate risk

### 1. Version bump type

- **Patch** (x.y.Z) - bug fixes, low risk
- **Minor** (x.Y.0) - new features, backwards-compatible by semver, low-medium risk
- **Major** (X.0.0) - potentially breaking, high risk - requires careful review

### 2. Changelog review

- Read the Dependabot PR body for release notes / changelog links
- If the PR body includes a changelog summary, check for breaking changes, deprecations, and behavioral changes
- For major bumps: `WebFetch` the changelog URL if provided

### 3. Usage impact

- Search the codebase for direct imports/usages of the package:
  - .NET: `grep -r "using.*{PackageNamespace}" src/backend/` and check `.csproj` references
  - Frontend: `grep -r "from ['\"].*{package}" src/frontend/src/`
- Check if the package is a transitive dependency (only in lock file, not directly referenced)
- For major bumps: check if any API surface we use was deprecated or removed

### 4. Test verification

- Checkout the PR branch: `gh pr checkout {number}`
- Run the relevant test suite:
  - Backend: `dotnet build src/backend/MyProject.slnx && dotnet test src/backend/MyProject.slnx -c Release`
  - Frontend: `cd src/frontend && pnpm install && pnpm run test && pnpm run check`
- If tests pass, that's a strong signal. If they fail, identify whether the failure is related to the update.

## Output format

```
## Dependabot Review: {package} {old} -> {new}

**Bump type**: patch / minor / major
**Scope**: backend / frontend
**Dependency type**: production / dev-only
**Test result**: PASS / FAIL

### Risk assessment

{Summary of findings - changelog highlights, breaking changes, usage impact}

### Verdict: SAFE TO MERGE / NEEDS MANUAL REVIEW / DO NOT MERGE

{Reasoning for verdict}
```

### Verdict criteria

| Verdict | When |
|---|---|
| **SAFE TO MERGE** | Patch/minor bump, tests pass, no breaking changes in changelog, no security advisories |
| **NEEDS MANUAL REVIEW** | Major bump with passing tests, or minor bump with deprecation warnings, or unfamiliar package with wide usage |
| **DO NOT MERGE** | Tests fail due to the update, known breaking changes that affect our usage, security concerns in the new version |

## Rules

- Research only - do NOT modify any files (except running `pnpm install` to update the lock file for testing)
- Always run the test suite - never skip it
- When in doubt, verdict is NEEDS MANUAL REVIEW, not SAFE TO MERGE
- A passing test suite does not override known breaking changes in the changelog
