Add an API endpoint to an existing feature.

## Input

Ask the user for:
1. **Feature name** (existing feature, e.g., `Authentication`, `Admin`, `Users`)
2. **Operation** (e.g., `GetOrders`, `UpdateStatus`)
3. **HTTP method and route** (e.g., `GET /api/v1/orders/{id}`)
4. **Request shape** (if POST/PUT/PATCH — fields with types)
5. **Response shape** (fields with types)
6. **Authorization** (public, authenticated, role-based?)

## Steps

Follow SKILLS.md "Add an Endpoint to an Existing Feature".

### 1. Application DTOs (if new shapes needed)

Create in `src/backend/MyProject.Application/Features/{Feature}/Dtos/`:
- `{Operation}Input.cs` — record with input fields
- `{Entity}Output.cs` — record with output fields (or reuse existing)

### 2. Service Interface

Add method to `src/backend/MyProject.Application/Features/{Feature}/I{Feature}Service.cs`:
- Return `Task<Result<T>>` or `Task<Result>` as appropriate
- Accept `CancellationToken cancellationToken = default` as last parameter
- Add `/// <summary>` XML docs

### 3. Service Implementation

Add method to `src/backend/MyProject.Infrastructure/Features/{Feature}/Services/{Feature}Service.cs`:
- Use `Result.Failure(ErrorMessages.X.Y)` for failures
- Use `TimeProvider` for time-dependent logic

### 4. WebApi DTOs

Create in `src/backend/MyProject.WebApi/Features/{Feature}/Dtos/{Operation}/`:
- `{Operation}Request.cs` — class with `init` properties, `[UsedImplicitly]`, `/// <summary>` on every property
- `{Operation}RequestValidator.cs` — FluentValidation rules

### 5. WebApi Response DTO (if new)

Create in `src/backend/MyProject.WebApi/Features/{Feature}/Dtos/`:
- `{Entity}Response.cs` — class with `init` properties, `[UsedImplicitly]`

### 6. Mapper

Add mapping methods to `src/backend/MyProject.WebApi/Features/{Feature}/{Feature}Mapper.cs`.

### 7. Controller Action

Add to `src/backend/MyProject.WebApi/Features/{Feature}/{Feature}Controller.cs`:
- `/// <summary>` + `/// <param>` (never for CancellationToken) + `/// <response>`
- `[HttpMethod("route")]`
- `[ProducesResponseType]` for every status code
- `ActionResult<T>` return type
- `CancellationToken cancellationToken` as last parameter

### 8. Verify

```bash
dotnet build src/backend/MyProject.slnx
```

### 9. Regenerate Frontend Types

If the backend is running:
```bash
cd src/frontend && npm run api:generate
```

### 10. Commit

```
feat({feature}): add {operation} endpoint
```

## Breaking Change Awareness

- **New endpoint:** Safe — additive change, no existing consumers
- **Modifying existing endpoint:** Check FILEMAP.md. If changing request/response shape:
  1. Consider if a v2 route is more appropriate
  2. Update frontend types and components in the same PR
  3. Document the breaking change in the commit body
- **Changing an existing service interface:** All implementations must be updated
