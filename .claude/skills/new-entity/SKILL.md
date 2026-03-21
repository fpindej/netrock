---
description: Create a backend entity with EF Core configuration and migration
user_invocable: true
---

Creates a new backend entity with EF Core configuration and migration.

Infers entity name, properties, feature name, and enum values from context. Asks only if the entity's purpose or key properties are genuinely ambiguous.

## Templates

Use these as starting points - fill in the specifics from context:
- [Entity class template](assets/entity.cs.md)
- [EF Core configuration template](assets/configuration.cs.md)

## Conventions

- **Boolean properties**: Use `Is*` prefix in C# (e.g. `IsUsed`, `IsInvalidated`) per .NET convention, but map to prefix-free DB column names via `HasColumnName` (e.g. `Used`, `Invalidated`) to keep the schema clean.

## Steps

**Domain:**

1. Create `src/backend/MyProject.Domain/Entities/{Entity}.cs`:
   - Extend `BaseEntity`, private setters, protected parameterless ctor, public ctor with `Id = Guid.NewGuid()`
2. If enums: create alongside entity with explicit integer values
3. Add error messages to `src/backend/MyProject.Shared/ErrorMessages.cs`

**Infrastructure:**

4. Create EF config `Infrastructure/Features/{Feature}/Configurations/{Entity}Configuration.cs`:
   - Extend `BaseEntityConfiguration<T>`, mark `internal`, `.HasComment()` on enum columns
   - For boolean properties: map with `.HasColumnName("PrefixFree")` to keep DB schema clean
5. Add `DbSet<{Entity}>` to `Infrastructure/Persistence/MyProjectDbContext.cs`
6. Run migration:
   ```bash
   dotnet ef migrations add Add{Entity} \
     --project src/backend/MyProject.Infrastructure \
     --startup-project src/backend/MyProject.WebApi \
     --output-dir Persistence/Migrations
   ```

**Dockerfile:**

7. If a new project was added that WebApi references, add its `.csproj` COPY line to the Dockerfile restore layer - otherwise `dotnet restore` fails in Docker builds.

**Verify and commit:**

8. `dotnet build src/backend/MyProject.slnx` - fix errors, loop until green
9. Commit: `feat({feature}): add {Entity} entity and EF configuration`

> This skill stops at Infrastructure. Use `/new-endpoint` to add service, controller, and API surface.
