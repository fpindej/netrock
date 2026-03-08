---
disable-model-invocation: true
---

Adds a typed options/configuration class.

## Steps

1. Create in the appropriate layer:
   - Infrastructure: `src/backend/Test.Infrastructure/{Area}/Options/{Name}Options.cs`
   - WebApi: `src/backend/Test.WebApi/Options/{Name}Options.cs`
2. Structure:
   ```csharp
   /// <summary>
   /// Configuration for {area}. Maps to "{SectionName}" in appsettings.json.
   /// </summary>
   public sealed class {Name}Options
   {
       public const string SectionName = "{Section}";

       /// <summary>
       /// Gets or sets the ...
       /// </summary>
       [Required]
       public string Value { get; init; } = string.Empty;
   }
   ```
3. Add section to `appsettings.json` (and `appsettings.Development.json` if dev differs - dev/test appsettings excluded from production publish via `StripDevConfig`)
4. Register in DI extension:
   ```csharp
   services.AddOptions<{Name}Options>()
       .BindConfiguration({Name}Options.SectionName)
       .ValidateDataAnnotations()
       .ValidateOnStart();
   ```
5. Update `docs/before-you-ship.md` if this is a must-configure production setting
