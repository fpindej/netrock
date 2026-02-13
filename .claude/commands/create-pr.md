Create a pull request for the current branch.

## Steps

### 1. Assess Current State

Run these in parallel:
- `git status` — check for uncommitted changes
- `git diff` — check for unstaged changes
- `git log master..HEAD --oneline` — see all commits on this branch
- `git diff master...HEAD --stat` — see all files changed vs master

If there are uncommitted changes, ask the user whether to commit them first.

### 2. Ensure Branch is Pushed

```bash
git push -u origin $(git branch --show-current)
```

### 3. Analyze Changes

Review ALL commits on the branch (not just the latest) to understand the full scope:
- What features/fixes are included?
- Which layers are affected (backend, frontend, both)?
- Are there breaking changes?

### 4. Draft PR

```bash
gh pr create --title "type(scope): description" --body "$(cat <<'EOF'
## Summary
- Bullet point 1
- Bullet point 2

## Changes
- List of significant changes

## Breaking Changes
<!-- Remove this section if none -->
- Description of any breaking changes and migration steps

## Test Plan
- [ ] Step 1 to verify
- [ ] Step 2 to verify

Closes #N
EOF
)" --label "label1,label2"
```

### PR Conventions

- **Title**: Conventional Commit format, under 70 chars
- **Base**: `master` (unless instructed otherwise)
- **Labels**: Apply all relevant labels (backend, frontend, feature, bug, etc.)
- **Linked issues**: Use `Closes #N` in the body

### Merge Strategy

This project uses **squash and merge** only:
```bash
gh pr merge <number> --squash \
  --subject "type(scope): description" \
  --body "Summary of changes. Closes #N"
```

## Output

Report the PR URL to the user.
