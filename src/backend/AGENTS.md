# Backend Conventions (.NET 10 / C# 13)

## Project Structure

```
src/backend/
├── MyProject.Shared/              # Result, ErrorType, ErrorMessages (zero deps)
├── MyProject.Domain/Entities/     # Business entities (BaseEntity)
├── MyProject.Application/         # Interfaces, DTOs, service contracts
│   ├── Features/{Feature}/I{Feature}Service.cs
│   ├── Features/{Feature}/Dtos/{Operation}Input.cs, {Entity}Output.cs
│   ├── Features/{Feature}/Persistence/I{Feature}Repository.cs  (optional)
│   └── Identity/Constants/AppRoles.cs, AppPermissions.cs
├── MyProject.Infrastructure/      # Implementations (all internal)
│   ├── Features/{Feature}/Services/, Configurations/, Extensions/
│   └── Persistence/MyProjectDbContext.cs
└── MyProject.WebApi/              # Entry point
    ├── Features/{Feature}/{Feature}Controller.cs, {Feature}Mapper.cs
    ├── Features/{Feature}/Dtos/{Operation}/{Operation}Request.cs + Validator
    ├── Authorization/             # RequirePermission, PermissionPolicyProvider
    └── Shared/                    # ApiController, ProblemFactory, ValidationConstants
```

## C# Conventions

### Access Modifiers

| Item | Modifier |
|---|---|
| Domain entities, Application interfaces/DTOs | `public` |
| Infrastructure services, EF configs, mappers | `internal` |
| WebApi controllers, request/response DTOs | `public` |

### Key Rules

- **Nullability**: `string.Empty` for required, `string?` for optional. Never `null!`.
- **Collections**: Return `IReadOnlyList<T>` by default. Never `List<T>` or `T[]` on public interfaces.
- **Time**: Always `TimeProvider` (injected). Registered as `TimeProvider.System` singleton.
- **XML docs**: `/// <summary>` on all public and internal API surface.
- **NuGet**: Versions in `Directory.Packages.props` only, versionless `<PackageReference>` in `.csproj`.

## Entity Definition

All entities extend `BaseEntity` (provides `Id`, `CreatedAt/By`, `UpdatedAt/By`, `IsDeleted`, `DeletedAt/By` — all set by `AuditingInterceptor` automatically).

Rules:
- Private setters, enforce invariants through methods
- Protected parameterless ctor for EF Core
- Public ctor generates `Id = Guid.NewGuid()`
- Boolean naming: `Is*`/`Has*` in C#, prefix-free column names via `HasColumnName`
- Soft delete via `entity.SoftDelete()` / `entity.Restore()` — never set `IsDeleted` directly

## Audit Trail

`AuditEvent` is append-only, does NOT extend `BaseEntity`. No FK on `UserId` (users are hard-deleted). Fire-and-forget logging — failures never break operations.

```csharp
// Use AuditActions constants — never raw strings
await auditService.LogAsync(AuditActions.AdminAssignRole, userId: callerId,
    targetEntityType: "User", targetEntityId: targetId,
    metadata: JsonSerializer.Serialize(new { role = input.Role }), ct: cancellationToken);
```

Always serialize metadata with `JsonSerializer.Serialize` — never string interpolation (JSON injection risk).

## EF Core

Configurations inherit `BaseEntityConfiguration<T>`, override `ConfigureEntity`. Mark `internal`. Auto-discovered via `ApplyConfigurationsFromAssembly()`.

- Default `public` schema. Named schemas only for existing grouped features (e.g., `"auth"`).
- `.HasComment()` on all enum columns documenting values.
- Seeding: roles via `AppRoles.All` (reflection), permissions via `SeedRolePermissionsAsync()`.

Migration command:
```bash
dotnet ef migrations add {Name} \
  --project src/backend/MyProject.Infrastructure \
  --startup-project src/backend/MyProject.WebApi \
  --output-dir Persistence/Migrations
```

## Result Pattern

```csharp
// Success
return Result<Guid>.Success(entity.Id);

// Static message — always use ErrorMessages constant
return Result.Failure(ErrorMessages.Admin.UserNotFound, ErrorType.NotFound);

// Runtime values go in server-side logs, never in client responses
logger.LogWarning("Operation failed for user '{UserId}': {Errors}", userId, errors);
return Result.Failure(ErrorMessages.Admin.DeleteFailed);
```

| ErrorType | HTTP | When |
|---|---|---|
| *(omit — default)* | 400 | Validation / business rule failures |
| `ErrorType.NotFound` | 404 | Entity not found |
| `ErrorType.Unauthorized` | 401 | Auth / token failures |

Controller: `ProblemFactory.Create(result.Error, result.ErrorType)` for failures.

## Service Pattern

1. **Application**: `public interface I{Feature}Service` + `record` DTOs (Input/Output)
2. **Infrastructure**: `internal class {Feature}Service(deps...) : I{Feature}Service` — primary ctor, `IOptions<T>` for config
3. **DI extension**: C# 13 `extension(IServiceCollection)` syntax, called from `Program.cs`

## DTO Naming

| Layer | Pattern | Example |
|---|---|---|
| WebApi Request | `{Operation}Request` | `LoginRequest` |
| WebApi Response | `{Entity}Response` | `UserResponse` |
| Application Input | `{Operation}Input` | `RegisterInput` |
| Application Output | `{Entity}Output` | `UserOutput` |

Mappers: `internal static class {Feature}Mapper` with extension methods (`request.ToInput()`, `output.ToResponse()`).

WebApi response DTOs: classes with `init` properties and `[UsedImplicitly]` from JetBrains.Annotations.

## Controller Conventions

- Authenticated endpoints extend `ApiController` (`[Authorize]`, route `api/v1/[controller]`)
- Public endpoints use `ControllerBase` directly (route `api/[controller]`)
- Always: `/// <summary>`, `[ProducesResponseType]` per status code, `CancellationToken` as last param
- Never `/// <param name="cancellationToken">` — it leaks into OAS `requestBody.description`
- File uploads: `[FromForm]` with `IFormFile`, `[Consumes("multipart/form-data")]`, `[RequestSizeLimit(bytes)]`
- Error responses: always `ProblemFactory.Create()` — never `NotFound()`, `BadRequest()`, `StatusCode(int, object)`, or anonymous objects
- Success responses: `Ok(response)`, `Created(string.Empty, response)` — never `CreatedAtAction`
- `[ProducesResponseType]` without `typeof(...)` on error codes (400, 401, 403, 404, 429) — ASP.NET auto-types as ProblemDetails

## Validation

FluentValidation auto-discovered from WebApi assembly. Co-locate validators with request DTOs.

| Rule Type | Convention |
|---|---|
| New passwords | `MinimumLength(6)` + lowercase + uppercase + digit rules |
| Existing passwords | `NotEmpty()` + `MaximumLength(255)` only |
| Optional fields | `.When(x => !string.IsNullOrEmpty(x.Field))` |
| URL fields | `Uri.TryCreate` + restrict to `http`/`https` schemes |
| Shared patterns | Extract to `ValidationConstants.cs` |

## Error Messages

- Static messages: `const string` in `ErrorMessages.cs` nested classes — always referenced by constant
- Dynamic messages: inline string interpolation in service
- Identity errors: pass `.Description` through directly (already user-friendly)

## Authorization

### Permission System

Atomic permissions via `[RequirePermission("permission.name")]` on controller actions. Permissions stored as role claims, embedded in JWT as `"permission"` claims.

- `AppPermissions.cs`: constants discovered via reflection (`AppPermissions.All`)
- `PermissionAuthorizationHandler`: SuperAdmin bypass → claim match → deny
- Never class-level `[Authorize(Roles)]` on controllers using permissions

### Role Hierarchy

`SuperAdmin` (3) > `Admin` (2) > `User` (1) > Custom (0). Enforced by Admin service:
- Cannot manage users at/above your rank
- Cannot assign/remove roles at/above your rank
- Cannot modify your own roles, lock yourself, or delete yourself

Permission changes on a role → invalidate refresh tokens + rotate security stamps + clear cache for all affected users.

## Repository Pattern

`IBaseEntityRepository<T>` provides CRUD with automatic soft-delete filtering (global query filter). Open generic registration covers basic entities.

Custom repositories: extend `IBaseEntityRepository<T>` in Application, implement in Infrastructure with `BaseEntityRepository<T>`. Return materialized objects only — never `IQueryable`.

Pagination: `Paginate(PaginatedRequest)` extension on `IQueryable<T>` returns `PaginatedResponse<T>`. Use in custom repository methods for list endpoints.

## Caching

`ICacheService` wraps `IDistributedCache` (Redis). Keys defined in `CacheKeys` constants. `UserCacheInvalidationInterceptor` auto-clears user cache on entity changes.

## File Storage

`IFileStorageService` — generic S3-compatible interface (`Upload`, `Download`, `Delete`, `Exists`). Implementation: `S3FileStorageService` (works with MinIO locally, any S3-compatible provider in production).

**Configuration:** `FileStorageOptions` in `appsettings.json` / env vars (`FileStorage__Endpoint`, `FileStorage__AccessKey`, etc.). `ForcePathStyle = true` for MinIO compatibility.

**Uploading files from a controller:**
1. Accept `IFormFile` via `[FromForm]` + `[Consumes("multipart/form-data")]` + `[RequestSizeLimit]`
2. Read to `byte[]` in the controller: `using var ms = new MemoryStream(); await file.CopyToAsync(ms); var data = ms.ToArray();`
3. Pass to the service for validation/processing (e.g., `IImageProcessingService` for avatar images)
4. Store via `fileStorageService.UploadAsync(key, data, contentType, ct)` — returns `Result`

**Storage keys:** Use `{feature}/{id}.{ext}` pattern (e.g., `avatars/{userId}.webp`). Overwrite replaces the old file.

**Avatar pattern:** `ApplicationUser.HasAvatar` boolean flag. Frontend constructs URL: `/api/users/{id}/avatar`. Backend serves via `File(data, contentType)` with `[ResponseCache(Duration = 300, Location = Client)]`.

**Swapping S3 provider:** Only `FileStorageOptions` changes — `S3FileStorageService` is provider-agnostic. Cloudflare R2, DigitalOcean Spaces, Backblaze B2 all work with path-style S3 API. Update `FileStorage__Endpoint`, credentials, and `UseSSL` in production env.

**Removing file storage:** Delete the `storage` Docker service, `FileStorage*` options/services/extensions, avatar endpoints, `HasAvatar` from `ApplicationUser`, and frontend avatar components. Remove `AWSSDK.S3` and `SkiaSharp` from `Directory.Packages.props`.

## OpenAPI

- `/// <summary>` on every controller action and DTO property → generates OAS descriptions
- `[ProducesResponseType]` declares all possible status codes per action
- `EnumSchemaTransformer` auto-documents enum values
- Scalar UI at `/scalar/v1` (development only)

## Options Pattern

```csharp
public sealed class {Name}Options
{
    public const string SectionName = "{Section}";

    /// <summary>Gets or sets the ...</summary>
    [Required]
    public string Value { get; init; } = string.Empty;
}
```

Register with `BindConfiguration`, `ValidateDataAnnotations`, `ValidateOnStart`.

Production build hygiene: `appsettings.Development.json` and `appsettings.Testing.json` excluded from publish via `StripDevConfig` (MSBuild + Dockerfile defense-in-depth).

## Testing

| Project | Tests | Dependencies |
|---|---|---|
| `Unit.Tests` | Pure logic (Shared, Domain, Application) | None — no mocks, no DI |
| `Component.Tests` | Service business logic | `TestDbContextFactory` (InMemory), `NSubstitute`, `IdentityMockHelpers` |
| `Api.Tests` | Full HTTP pipeline (routes, auth, status codes) | `CustomWebApplicationFactory`, `TestAuthHandler` |
| `Architecture.Tests` | Layer deps, naming, visibility | NetArchTest |

API test auth: `"Authorization", "Test"` (basic user), `TestAuth.WithPermissions(...)` (specific perms), `TestAuth.SuperAdmin()`.

Response contracts: frozen records in `Contracts/ResponseContracts.cs` — deserialize and assert key fields for 200/201 responses.

## Hosting

`HostingOptions` controls `ForceHttps` (default true — required behind TLS proxy) and `ReverseProxy` trust (trusted networks/proxies for `X-Forwarded-For`).

Docker local: `172.16.0.0/12` pre-configured. Set `XFF_DEPTH=1` on frontend container.
