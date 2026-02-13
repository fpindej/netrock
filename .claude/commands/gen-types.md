Regenerate frontend API types from the backend OpenAPI spec.

## Prerequisites

The backend must be running. Check with:
```bash
curl -s http://localhost:8080/openapi/v1.json | head -1
```

If not running, start it:
```bash
docker compose -f docker-compose.local.yml up -d api
```

Wait for it to be ready, then proceed.

## Steps

### 1. Regenerate

```bash
cd src/frontend && npm run api:generate
```

### 2. Review Changes

Read `src/frontend/src/lib/api/v1.d.ts` and identify what changed:
- New schemas/endpoints (safe — additive)
- Modified schemas (potentially breaking — check downstream)
- Removed schemas/endpoints (breaking — find and update all usages)

### 3. Update Type Aliases

Check `src/frontend/src/lib/types/index.ts`:
- Add aliases for new commonly-used schemas
- Update aliases if schema names changed
- Remove aliases for deleted schemas

### 4. Fix Type Errors

```bash
cd src/frontend && npm run check
```

If errors found:
- Update components that reference changed types
- Update API calls that use changed endpoints
- Update form fields that map to changed request shapes

### 5. Format

```bash
cd src/frontend && npm run format
```

### 6. Commit

```
chore(frontend): regenerate API types
```

Include the `v1.d.ts` changes with the backend changes that caused them (same commit or same PR).

## Breaking Change Awareness

- If `npm run check` reveals type errors after regeneration, the backend made a breaking change to the API contract
- Fix all type errors before committing — never commit broken types
- If many components break, consider whether the backend change should be reverted or versioned (v2 endpoint)
