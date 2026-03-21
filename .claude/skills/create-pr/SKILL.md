---
description: Create a pull request for the current branch
user-invocable: true
argument-hint: "[base branch]"
---

Creates a pull request for the current branch.

## Current Branch Context

**Branch:** !`git branch --show-current`

**Commits on this branch:**
!`git log master..HEAD --oneline 2>/dev/null || echo "(no commits ahead of master)"`

**Files changed:**
!`git diff --stat master 2>/dev/null || echo "(no diff from master)"`

## Hard rules

- **Never commit on master.** If on master, create a branch first. No exceptions.
- **No Co-Authored-By lines** in commits. User mentions Claude on PRs, not on individual commits.
- **Sign all commits** with `-S` flag.
- **Session docs are immutable history** - never update old session docs to reflect current state.

## Steps

1. Check `git status` - if uncommitted changes exist, commit them first (infer message from changes)
2. Verify you are NOT on master. If on master, stop and ask the user to create a branch.
3. Review all branch commits: `git log master..HEAD --oneline`
4. Push if needed: `git push -u origin $(git branch --show-current)`

**Session documentation (auto-generate for non-trivial PRs - 3+ commits or 5+ files changed):**

5. Create `docs/sessions/{YYYY-MM-DD}-{topic-slug}.md` per `docs/sessions/README.md`:
   - Summarize all commits on the branch
   - List files changed with reasons
   - Document key decisions and reasoning
   - Add follow-up items if any
6. Commit: `docs: add session notes for {topic}`
7. Push again

**Create PR:**

8. Create PR with `gh pr create` using the [PR body template](assets/pr-body.md):
   - **Title**: Conventional Commit format, under 70 chars
   - **Base**: argument if provided, otherwise `master`
   - **Labels**: Apply all relevant (`backend`, `frontend`, `feature`, `bug`, `security`, `documentation`)
9. Merge strategy for this project: **squash-and-merge only**
10. Report PR URL

## Stacked PRs

When creating stacked PRs (PR2 based on PR1's branch), use the `--base` flag to set the correct base branch. After amending PR1, rebase PR2 with `git rebase feat/pr1-branch` - the old PR1 commit is auto-skipped.
