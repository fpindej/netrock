---
description: Add a new project area to CI workflows
user_invocable: true
---

Adds a new project area to CI.

## Steps

1. Open `.github/workflows/ci.yml`
2. Add a path filter in the `changes` job:
   ```yaml
   mobile:
     - 'src/mobile/**'
   ```
3. Add an output to the `changes` job:
   ```yaml
   mobile: ${{ steps.filter.outputs.mobile }}
   ```
4. Add a new job (copy an existing one and adapt):
   ```yaml
   mobile-checks:
     name: Mobile checks
     needs: changes
     if: needs.changes.outputs.mobile == 'true'
     runs-on: ubuntu-latest
     timeout-minutes: 10
     defaults:
       run:
         working-directory: src/mobile
     steps:
       - uses: actions/checkout@v6
       # ... setup + build + lint + test steps
   ```
5. Add the new job to the gate job's `needs`:
   ```yaml
   ci-passed:
     needs: [backend-build, frontend-checks, mobile-checks]
   ```
6. *(If the project has a Dockerfile)* Add a corresponding job to `.github/workflows/docker.yml` and update its `on.paths` and `dorny/paths-filter` filters
7. Verify: push a branch and confirm the new job appears in the PR checks

No branch protection changes needed - the `CI passed` gate job covers all upstream jobs automatically.
