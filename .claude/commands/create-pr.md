Create a pull request for the current branch.

## Steps

1. Check `git status` — if uncommitted changes exist, commit them first (infer message from changes)
2. Review all branch commits: `git log master..HEAD --oneline`
3. Push if needed: `git push -u origin $(git branch --show-current)`

**Session documentation (auto-generate):**

4. Create or update `docs/sessions/{YYYY-MM-DD}-{topic-slug}.md` per `docs/sessions/README.md`:
   - Summarize all commits on the branch
   - List files changed with reasons
   - Document key decisions and reasoning
   - Add follow-up items if any
5. Commit: `docs: add session notes for {topic}`
6. Push again

**Create PR:**

7. Create PR with `gh pr create`:
   - **Title**: Conventional Commit format, under 70 chars
   - **Base**: `master`
   - **Labels**: Apply all relevant (`backend`, `frontend`, `feature`, `bug`, `security`, `documentation`)
   - **Body**:
     ```
     ## Summary
     - Change 1
     - Change 2

     ## Breaking Changes
     None / describe if any

     ## Test Plan
     - [ ] Verification steps

     Closes #N (if applicable)
     ```
8. Merge strategy for this project: **squash-and-merge only**
9. Report PR URL
