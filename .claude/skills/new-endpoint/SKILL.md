---
disable-model-invocation: true
---

Adds an API endpoint to an existing feature.

Infers the feature, operation, HTTP method, route, shapes, and auth from context. Asks only if multiple valid approaches exist and the tradeoffs matter.

## Steps

1. Determine: feature, operation name, HTTP method, route, request/response shape, auth requirements
2. Check FILEMAP.md for downstream impact if modifying existing endpoints

**Create files (match existing feature patterns - read them first):**

3. Application DTOs (if new): `Application/Features/{Feature}/Dtos/{Operation}Input.cs` / `{Entity}Output.cs`
4. Add method to `Application/Features/{Feature}/I{Feature}Service.cs`
5. Implement in `Infrastructure/Features/{Feature}/Services/{Feature}Service.cs`
6. WebApi request/response DTOs (if new): `WebApi/Features/{Feature}/Dtos/{Operation}/`
7. Add mapper methods to `WebApi/Features/{Feature}/{Feature}Mapper.cs`
8. Add controller action - include `/// <summary>`, `[ProducesResponseType]`, `CancellationToken`
9. Add FluentValidation validator co-located with request DTO
10. Write tests: component test for service, API integration test for endpoint, validator test

**Dockerfile:**

11. If a new project was added that WebApi references, add its `.csproj` COPY line to the Dockerfile restore layer - otherwise `dotnet restore` fails in Docker builds.

**Verify and commit:**

12. `dotnet build src/backend/MyProject.slnx` - fix errors, loop until green
13. `dotnet test src/backend/MyProject.slnx -c Release` - fix failures, loop until green
14. Commit: `feat({feature}): add {operation} endpoint`
15. Regenerate frontend types: `cd src/frontend && pnpm run api:generate` - fix any type errors
16. Commit type changes if applicable
