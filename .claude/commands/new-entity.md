Create a new backend entity with EF Core configuration and migration.

Ask the user: **What entity?** (e.g., "Order with name, amount, status"). Infer feature name from entity name. Ask about enum values only if enum properties are mentioned.

## Steps

**Domain:**

1. Create `src/backend/MyProject.Domain/Entities/{Entity}.cs`:
   - Extend `BaseEntity`, private setters, protected parameterless ctor, public ctor with `Id = Guid.NewGuid()`
2. If enums: create alongside entity with explicit integer values
3. Add error messages to `src/backend/MyProject.Shared/ErrorMessages.cs`

**Infrastructure:**

4. Create EF config `Infrastructure/Features/{Feature}/Configurations/{Entity}Configuration.cs`:
   - Extend `BaseEntityConfiguration<T>`, mark `internal`, `.HasComment()` on enum columns
5. Add `DbSet<{Entity}>` to `Infrastructure/Persistence/MyProjectDbContext.cs`
6. Run migration:
   ```bash
   dotnet ef migrations add Add{Entity} \
     --project src/backend/MyProject.Infrastructure \
     --startup-project src/backend/MyProject.WebApi \
     --output-dir Persistence/Migrations
   ```

**Verify:**

7. `dotnet build src/backend/MyProject.slnx` — fix any errors before committing
8. Commit: `feat({feature}): add {Entity} entity and EF configuration`

> This command stops at Infrastructure. Use `/new-endpoint` to add service, controller, and API surface.
