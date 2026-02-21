Create a new backend entity with EF Core configuration and migration.

Infer entity name, properties, feature name, and enum values from context. Ask only if the entity's purpose or key properties are genuinely ambiguous.

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

**Verify and commit:**

7. `dotnet build src/backend/MyProject.slnx` — fix errors, loop until green
8. Commit: `feat({feature}): add {Entity} entity and EF configuration`

> This command stops at Infrastructure. Use `/new-endpoint` to add service, controller, and API surface.
