# Backend API Rules

## Error Handling
- `Result`/`Result<T>` for all fallible operations - never throw for business logic
- `ProblemFactory.Create(result.Error, result.ErrorType)` for error responses - never `NotFound()`, `BadRequest()`, or anonymous objects
- Client-facing error messages: `ErrorMessages.*` constants only - runtime values go in `ILogger`, never in `Result.Failure()`

## Controllers
- Authenticated endpoints extend `ApiController` - public endpoints use `ControllerBase` directly
- Always: `/// <summary>`, `[ProducesResponseType]` per status code, `CancellationToken` as last param
- Never `/// <param name="cancellationToken">` - it leaks into OpenAPI `requestBody.description`
- `[ProducesResponseType]` without `typeof(...)` on error codes (400, 401, 403, 404, 429)

## Services & DI
- All Infrastructure implementations are `internal` - Application interfaces are `public`
- DI extensions use C# 13 `extension(IServiceCollection)` syntax
- `TimeProvider` (injected) - never `DateTime.UtcNow` or `DateTimeOffset.UtcNow`
- `System.Text.Json` only - never `Newtonsoft.Json`

## Authorization
- `[RequirePermission("permission.name")]` on actions - never class-level `[Authorize(Roles)]` on permission controllers
- Role hierarchy enforced: cannot manage users at/above your rank

## Entities
- Extend `BaseEntity`, private setters, invariants via methods, protected parameterless ctor for EF
- Soft delete via `entity.SoftDelete()` / `entity.Restore()` - never set `IsDeleted` directly
- Never `null!` - fix the design instead
