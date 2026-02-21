Create a GitHub issue. Ask the user what needs to be done if not already clear from context.

## Steps

1. Determine title in Conventional Commit format: `type(scope): description`
   - Infer type (`feat`, `fix`, `refactor`, `chore`, `docs`) and scope from the description
2. Write a body: problem, proposed approach, affected files/areas
3. Apply labels — all that fit: `backend`, `frontend`, `security`, `feature`, `bug`, `documentation`
4. If the work crosses stack boundary (backend + frontend) or has multiple independent deliverables, split into sub-issues:
   ```bash
   gh issue create --title "..." --body "..." --label "..."
   # Get sub-issue numeric ID
   gh api --method GET /repos/{owner}/{repo}/issues/{sub_number} --jq '.id'
   # Link to parent
   gh api --method POST /repos/{owner}/{repo}/issues/{parent_number}/sub_issues --field sub_issue_id={id}
   ```
5. For small, tightly coupled changes — create a single issue (don't over-split)
6. Report created URL(s)
