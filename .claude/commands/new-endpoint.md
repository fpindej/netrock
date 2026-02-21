Add an API endpoint to an existing feature.

Infer the feature, operation, HTTP method, route, shapes, and auth from context. Ask only if multiple valid approaches exist and the tradeoffs matter.

## Steps

1. Determine: feature, operation name, HTTP method, route, request/response shape, auth requirements
2. Check FILEMAP.md for downstream impact if modifying existing endpoints

**Create files (match existing feature patterns — read them first):**

3. Application DTOs (if new): `Application/Features/{Feature}/Dtos/{Operation}Input.cs` / `{Entity}Output.cs`
4. Add method to `Application/Features/{Feature}/I{Feature}Service.cs`
5. Implement in `Infrastructure/Features/{Feature}/Services/{Feature}Service.cs`
6. WebApi request/response DTOs (if new): `WebApi/Features/{Feature}/Dtos/{Operation}/`
7. Add mapper methods to `WebApi/Features/{Feature}/{Feature}Mapper.cs`
8. Add controller action — include `/// <summary>`, `[ProducesResponseType]`, `CancellationToken`
9. Add FluentValidation validator co-located with request DTO
10. Write tests: component test for service, API integration test for endpoint, validator test

**Verify and commit:**

11. `dotnet build src/backend/MyProject.slnx` — fix errors, loop until green
12. `dotnet test src/backend/MyProject.slnx -c Release` — fix failures, loop until green
13. Commit: `feat({feature}): add {operation} endpoint`
14. Regenerate frontend types: `cd src/frontend && pnpm run api:generate` — fix any type errors
15. Commit type changes if applicable
