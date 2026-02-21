Add an API endpoint to an existing feature.

Ask the user: **What does this endpoint do?** (e.g., "list orders", "update user status"). Infer the rest — ask only if genuinely ambiguous.

## Steps

1. Determine: feature, operation name, HTTP method, route, request/response shape, auth requirements
2. Check FILEMAP.md if modifying an existing endpoint (breaking change?)

**Create files (follow existing feature patterns):**

3. Application DTOs (if new): `Application/Features/{Feature}/Dtos/{Operation}Input.cs` / `{Entity}Output.cs`
4. Add method to `Application/Features/{Feature}/I{Feature}Service.cs`
5. Implement in `Infrastructure/Features/{Feature}/Services/{Feature}Service.cs`
6. WebApi request/response DTOs (if new): `WebApi/Features/{Feature}/Dtos/{Operation}/`
7. Add mapper methods to `WebApi/Features/{Feature}/{Feature}Mapper.cs`
8. Add controller action — include `/// <summary>`, `[ProducesResponseType]`, `CancellationToken`
9. Add FluentValidation validator co-located with request DTO
10. Add tests: component test for service, API integration test for endpoint, validator test

**Verify:**

11. `dotnet build src/backend/MyProject.slnx` — fix any errors
12. `dotnet test src/backend/MyProject.slnx -c Release` — fix any failures
13. Commit: `feat({feature}): add {operation} endpoint`

**If backend is running:** regenerate frontend types with `/gen-types`
