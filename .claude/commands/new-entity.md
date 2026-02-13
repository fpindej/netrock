Create a new backend entity with EF Core configuration and migration.

## Input

Ask the user for:
1. **Entity name** (PascalCase, e.g., `Order`)
2. **Properties** (name, type, nullability)
3. **Feature name** (usually same as entity, but may differ)
4. **Any enum properties** (if yes, ask for enum members with explicit integer values)

## Steps

Follow SKILLS.md "Add an Entity (End-to-End)" — **Domain + Infrastructure layers only** (stop before Application/WebApi).

### 1. Domain Entity

Create `src/backend/MyProject.Domain/Entities/{Entity}.cs`:
- Extend `BaseEntity`
- Private setters on all properties
- Protected parameterless constructor for EF Core
- Public constructor generating `Id = Guid.NewGuid()`
- If enum properties, create enum file alongside with explicit integer values

### 2. Error Messages

Add error message constants to `src/backend/MyProject.Domain/ErrorMessages.cs` in a new nested class.

### 3. EF Configuration

Create `src/backend/MyProject.Infrastructure/Features/{Feature}/Configurations/{Entity}Configuration.cs`:
- Extend `BaseEntityConfiguration<T>`, override `ConfigureEntity`
- Mark `internal`
- Set table name with `builder.ToTable("{table_name}")`
- Add `.HasComment()` on enum columns with format `EnumType enum: 0=Value, 1=Value`
- Add appropriate constraints (maxlength, precision, required, unique indexes)

### 4. DbSet

Add `DbSet<{Entity}>` to `src/backend/MyProject.Infrastructure/Persistence/MyProjectDbContext.cs`.

### 5. Migration

Run:
```bash
dotnet ef migrations add Add{Entity} \
  --project src/backend/MyProject.Infrastructure \
  --startup-project src/backend/MyProject.WebApi \
  --output-dir Features/Postgres/Migrations
```

### 6. Verify

```bash
dotnet build src/backend/MyProject.slnx
```

### 7. Commit

```
feat({feature}): add {Entity} entity and EF configuration
```

## Breaking Change Awareness

- Adding a new entity is safe — no existing code depends on it yet
- Adding a DbSet to DbContext requires a migration but doesn't break existing tables
- Check FILEMAP.md if modifying an existing entity instead of creating a new one
