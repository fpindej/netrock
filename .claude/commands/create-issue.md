Create a GitHub issue with proper labels and optional sub-issues.

## Input

Ask the user for:
1. **Issue title** (or describe the problem/feature and I'll draft one)
2. **Type** — `feat`, `fix`, `refactor`, `chore`, `docs`
3. **Scope** — which area (e.g., `auth`, `orders`, `frontend`)
4. **Description** — problem, proposed solution, affected files
5. **Should it be split into sub-issues?** (crosses stack boundary, multiple deliverables, parallelizable)

## Steps

### Single Issue

```bash
gh issue create \
  --title "type(scope): description" \
  --body "..." \
  --label "label1,label2"
```

### With Sub-Issues

For multi-layer work, create a parent + sub-issues linked via the GitHub Sub-Issues API:

1. Create the parent issue
2. Create each sub-issue
3. Get the sub-issue's numeric ID:
   ```bash
   gh api --method GET /repos/{owner}/{repo}/issues/{sub_issue_number} --jq '.id'
   ```
4. Link each sub-issue to the parent:
   ```bash
   gh api --method POST /repos/{owner}/{repo}/issues/{parent_number}/sub_issues \
     --field sub_issue_id={sub_issue_id}
   ```

### Labels

Apply **all** relevant labels:

| Label | Use when |
|---|---|
| `backend` | Changes touch `src/backend/` |
| `frontend` | Changes touch `src/frontend/` |
| `security` | Security-related changes |
| `feature` | New feature or enhancement |
| `bug` | Fixing incorrect behavior |
| `documentation` | Documentation changes |

### When to Split

- Crosses stack boundary (backend + frontend)
- Independent deliverables (entity + service + controller could each be reviewed alone)
- Multiple logical concerns (entity + API + UI + i18n)
- Parallelizable work

### When NOT to Split

- Small, tightly coupled changes that only make sense together
- Single-layer fixes that take one commit

## Output

Report the created issue URL(s) to the user.
