---
description: Regenerate frontend API types from the backend OpenAPI spec
user-invocable: true
---

Regenerates frontend API types from the backend OpenAPI spec.

**Recently changed backend files:**
!`git diff --name-only HEAD~3 -- src/backend/ 2>/dev/null | grep -E '\.(cs)$' | head -20 || echo "(no recent backend changes)"`

## Steps

1. Try generating types:
   ```bash
   cd src/frontend && pnpm run api:generate
   ```

2. **If generation fails** (backend not running, generator broken, any reason): don't loop retrying or wait for startup. If you can see the API definition clearly from the backend code (DTOs, controllers, response types), edit `src/frontend/src/lib/api/v1.d.ts` manually to match. Add a comment at the top of the commit message: `Note: v1.d.ts was manually edited - regenerate with pnpm run api:generate to verify.`

3. Check what changed - look for renamed/removed schemas (breaking) vs added schemas (safe)

4. Update type aliases in `src/frontend/src/lib/types/index.ts` if schemas changed

5. Fix type errors:
   ```bash
   cd src/frontend && pnpm run check
   ```
   If errors: the backend made a breaking API change - fix all frontend consumers

6. Format: `cd src/frontend && pnpm run format`

7. Commit `v1.d.ts` with the backend changes that caused the regeneration
