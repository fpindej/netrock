# PR Body Template

Use this structure for all pull request descriptions.

```markdown
## Summary
- {Change 1 - what and why}
- {Change 2 - what and why}
- {Change 3 - what and why}

## Breaking Changes
None / {describe if any, including migration steps}

## Test Plan
- [ ] {Verification step 1}
- [ ] {Verification step 2}
- [ ] Backend: `dotnet build && dotnet test -c Release`
- [ ] Frontend: `pnpm run test && pnpm run check`
```

## Guidelines

- **Summary**: Bullet points, focus on "what changed and why" not "which files"
- **Breaking Changes**: Required section. "None" if no breaking changes. If breaking, describe the migration path.
- **Test Plan**: Concrete steps a reviewer can follow. Always include the verification commands.
- Keep the title under 70 chars, Conventional Commit format
- Add labels: `backend`, `frontend`, `feature`, `bug`, `security`, `documentation`
- For stacked PRs: set `--base` to the parent branch
