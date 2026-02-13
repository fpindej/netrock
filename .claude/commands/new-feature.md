Create a full-stack feature: backend entity through to frontend page.

## Input

Ask the user for:
1. **Feature name** (e.g., `Orders`)
2. **Entity properties** (name, type, nullability, enums)
3. **Endpoints needed** (CRUD or custom — methods, routes, request/response shapes)
4. **Frontend page details** (route, components, data display)
5. **Authorization** (public, authenticated, role-based?)

## Steps

Follow SKILLS.md "Add a Full-Stack Feature". This combines multiple skills in order.

### Phase 1 — Backend Domain + Infrastructure

1. Create entity: `src/backend/MyProject.Domain/Entities/{Entity}.cs`
2. Create enums (if any) with explicit integer values
3. Add error messages to `src/backend/MyProject.Domain/ErrorMessages.cs`
4. Create EF config: `src/backend/MyProject.Infrastructure/Features/{Feature}/Configurations/`
5. Add DbSet to `src/backend/MyProject.Infrastructure/Persistence/MyProjectDbContext.cs`

**Commit:** `feat({feature}): add {Entity} entity and EF configuration`

### Phase 2 — Backend Application Layer

6. Create service interface: `src/backend/MyProject.Application/Features/{Feature}/I{Feature}Service.cs`
7. Create DTOs: `src/backend/MyProject.Application/Features/{Feature}/Dtos/`
8. *(Optional)* Create repository interface if custom queries needed

**Commit:** `feat({feature}): add I{Feature}Service and DTOs`

### Phase 3 — Backend Infrastructure Services

9. Implement service: `src/backend/MyProject.Infrastructure/Features/{Feature}/Services/`
10. *(Optional)* Implement repository
11. Create DI extension: `src/backend/MyProject.Infrastructure/Features/{Feature}/Extensions/`

**Commit:** `feat({feature}): implement {Feature}Service`

### Phase 4 — Backend WebApi

12. Create controller: `src/backend/MyProject.WebApi/Features/{Feature}/`
13. Create request/response DTOs with `/// <summary>` on every property
14. Create mapper
15. Create validators
16. Wire DI in `src/backend/MyProject.WebApi/Program.cs`

**Commit:** `feat({feature}): add {Feature}Controller with endpoints`

### Phase 5 — Migration

17. Run migration:
```bash
dotnet ef migrations add Add{Entity} \
  --project src/backend/MyProject.Infrastructure \
  --startup-project src/backend/MyProject.WebApi \
  --output-dir Features/Postgres/Migrations
```
18. Verify: `dotnet build src/backend/MyProject.slnx`

**Commit:** `feat({feature}): add {Entity} migration`

### Phase 6 — Frontend (backend must be running)

19. Regenerate types: `cd src/frontend && npm run api:generate`
20. Add type alias to `src/frontend/src/lib/types/index.ts`
21. Create components: `src/frontend/src/lib/components/{feature}/` with barrel
22. Create page: `src/frontend/src/routes/(app)/{feature}/`
23. Add server load if needed
24. Add i18n keys to `en.json` and `cs.json`
25. Update `SidebarNav.svelte` with navigation entry
26. Verify: `cd src/frontend && npm run format && npm run lint && npm run check`

**Commit:** `feat({feature}): add {feature} page in frontend`

## Breaking Change Awareness

- Full-stack features are additive — they don't break existing code
- Check FILEMAP.md if the feature touches existing entities or endpoints
- If extending an existing entity with new properties, a migration is needed but data is preserved (nullable columns or defaults)
- Verify the migration doesn't drop or alter existing columns

## Verification Checklist

- [ ] `dotnet build src/backend/MyProject.slnx` passes
- [ ] `cd src/frontend && npm run format && npm run lint && npm run check` passes
- [ ] Migration creates expected tables/columns (review the migration file)
- [ ] Frontend types regenerated and no type errors
- [ ] Navigation entry appears in sidebar
- [ ] i18n keys present in both language files
