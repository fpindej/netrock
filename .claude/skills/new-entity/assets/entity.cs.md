# Entity Template

```csharp
using MyProject.Domain.Entities;

namespace MyProject.Domain.Entities;

/// <summary>
/// Represents a {EntityDescription}.
/// </summary>
public class {Entity} : BaseEntity
{
    /// <summary>
    /// Required by EF Core for materialization.
    /// </summary>
    protected {Entity}() { }

    /// <summary>
    /// Initializes a new <see cref="{Entity}"/>.
    /// </summary>
    public {Entity}({CtorParams})
    {
        Id = Guid.NewGuid();
        // Set properties from ctor params
    }

    /// <summary>
    /// Gets the {propertyDescription}.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    // Boolean example:
    // /// <summary>
    // /// Gets a value indicating whether the {entity} is active.
    // /// </summary>
    // public bool IsActive { get; private set; }

    // Enum example:
    // /// <summary>
    // /// Gets the status of the {entity}.
    // /// </summary>
    // public {Entity}Status Status { get; private set; }

    // Navigation example:
    // /// <summary>
    // /// Gets the collection of related items.
    // /// </summary>
    // public ICollection<RelatedEntity> Items { get; private set; } = [];

    // Domain method example:
    // /// <summary>
    // /// Marks the {entity} as completed.
    // /// </summary>
    // public void Complete()
    // {
    //     Status = {Entity}Status.Completed;
    // }
}
```

## Rules

- Private setters, enforce invariants through methods
- Protected parameterless ctor for EF Core
- `Id = Guid.NewGuid()` in the public ctor
- Boolean naming: `Is*`/`Has*` in C#
- `/// <summary>` on all public members
- `string.Empty` for required strings, `string?` for optional
- `ICollection<T>` initialized to `[]` for collection navs
