# EF Core Configuration Template

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyProject.Domain.Entities;
using MyProject.Infrastructure.Persistence.Configurations;

namespace MyProject.Infrastructure.Features.{Feature}.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="{Entity}"/> entity.
/// </summary>
internal class {Entity}Configuration : BaseEntityConfiguration<{Entity}>
{
    /// <inheritdoc />
    protected override void ConfigureEntity(EntityTypeBuilder<{Entity}> builder)
    {
        // String property with max length
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        // Boolean property - map to prefix-free column name
        // builder.Property(x => x.IsActive)
        //     .HasColumnName("Active")
        //     .IsRequired()
        //     .HasDefaultValue(false);

        // Enum property - always add HasComment documenting values
        // builder.Property(x => x.Status)
        //     .IsRequired()
        //     .HasConversion<string>()
        //     .HasMaxLength(50)
        //     .HasComment("Values: Draft, Active, Archived");

        // Foreign key
        // builder.HasOne(x => x.Owner)
        //     .WithMany(u => u.Items)
        //     .HasForeignKey(x => x.OwnerId)
        //     .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        // builder.HasIndex(x => x.Name).IsUnique();
        // builder.HasIndex(x => x.OwnerId);
    }
}
```

## Rules

- Extend `BaseEntityConfiguration<T>` (handles Id, audit columns, soft-delete)
- Mark `internal`
- Boolean columns: `.HasColumnName("PrefixFree")` to drop `Is`/`Has` prefix
- Enum columns: `.HasConversion<string>()` + `.HasComment("Values: ...")` always
- Default `public` schema unless the feature belongs to an existing named schema
- Auto-discovered via `ApplyConfigurationsFromAssembly()`
