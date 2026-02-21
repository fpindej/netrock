Create a pull request for the current branch.

## Steps

1. Check `git status` — if uncommitted changes exist, commit them first (ask user for message if unclear)
2. Review all branch commits: `git log master..HEAD --oneline`
3. Push if needed: `git push -u origin $(git branch --show-current)`
4. Create PR:
   ```bash
   gh pr create \
     --title "type(scope): description" \
     --base master \
     --label "backend,frontend,..." \
     --body "## Summary
   - Change 1
   - Change 2

   ## Breaking Changes
   None / describe if any

   ## Test Plan
   - [ ] Verification steps

   Closes #N (if applicable)"
   ```
5. Title: Conventional Commit format, under 70 chars
6. Labels: all that fit (`backend`, `frontend`, `feature`, `bug`, `security`, `documentation`)
7. Merge strategy for this project: **squash-and-merge only** (`gh pr merge --squash`)
8. Report PR URL
