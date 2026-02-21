Regenerate frontend API types from the backend OpenAPI spec.

## Steps

1. Verify backend is running:
   ```bash
   curl -sf http://localhost:8080/openapi/v1.json > /dev/null || echo "Backend not running"
   ```
   If not running: `docker compose -f docker-compose.local.yml up -d api` and wait ~10s

2. Generate types:
   ```bash
   cd src/frontend && pnpm run api:generate
   ```

3. Check what changed — look for renamed/removed schemas (breaking) vs added schemas (safe)

4. Update type aliases in `src/frontend/src/lib/types/index.ts` if schemas changed

5. Fix type errors:
   ```bash
   cd src/frontend && pnpm run check
   ```
   If errors: the backend made a breaking API change — fix all frontend consumers

6. Format: `cd src/frontend && pnpm run format`

7. Commit `v1.d.ts` with the backend changes that caused the regeneration
