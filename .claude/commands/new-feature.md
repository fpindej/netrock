Create a full-stack feature: backend entity through to frontend page.

Infer everything possible from the feature description. Ask only when genuinely ambiguous (multiple valid designs with different tradeoffs). Prefer conventions from existing features.

## Steps

This chains entity → service/API → frontend. Commit atomically after each logical unit.

**Backend — Entity (see SKILLS.md "Add an Entity"):**

1. Domain: entity + enums + error messages
2. Infrastructure: EF config + DbSet + migration
3. Verify: `dotnet build src/backend/MyProject.slnx` — loop until green
4. Commit: `feat({feature}): add {Entity} entity and EF configuration`

**Backend — Service & API (see SKILLS.md "Add an Endpoint"):**

5. Application: `I{Feature}Service` + Input/Output DTOs
6. Infrastructure: `{Feature}Service` (internal) + DI extension
7. WebApi: controller + request/response DTOs + mapper + validators + Program.cs wiring
8. Write tests: component, API integration, validator
9. Verify: `dotnet test src/backend/MyProject.slnx -c Release` — loop until green
10. Commit: `feat({feature}): add {Feature} service and API endpoints`

**Frontend (see SKILLS.md "Add a Page"):**

11. Regenerate types: `cd src/frontend && pnpm run api:generate`
12. Add type alias to `src/frontend/src/lib/types/index.ts`
13. Create components in `$lib/components/{feature}/` with barrel `index.ts`
14. Create page in `routes/(app)/{feature}/`
15. Add i18n keys to both `en.json` and `cs.json`
16. Add sidebar navigation entry
17. Verify: `cd src/frontend && pnpm run format && pnpm run lint && pnpm run check` — loop until green
18. Commit: `feat({feature}): add {feature} frontend page`

**Always:** If build/check fails, read the error, fix it, re-run. Never stop to report a fixable error.
