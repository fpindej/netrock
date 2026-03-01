# Backend Conventions (.NET 10 / C# 13)

## Project Structure

```
src/backend/
├── MyProject.Shared/              # Result, ErrorType, ErrorMessages (zero deps)
├── MyProject.Domain/Entities/     # Business entities (BaseEntity)
├── MyProject.Application/         # Interfaces, DTOs, service contracts
│   ├── Features/{Feature}/I{Feature}Service.cs
│   ├── Features/{Feature}/Dtos/{Operation}Input.cs, {Entity}Output.cs
│   ├── Identity/IUserService.cs, IUserContext.cs
│   └── Identity/Constants/AppRoles.cs, AppPermissions.cs
├── MyProject.Infrastructure/      # Implementations (all internal)
│   ├── Features/{Feature}/Services/, Configurations/, Extensions/
│   └── Persistence/MyProjectDbContext.cs
├── MyProject.ServiceDefaults/     # Aspire shared: OTEL, service discovery, resilience
├── MyProject.AppHost/             # Aspire orchestrator (local dev only)
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

- **Nullability**: `string.Empty` for required, `string?` for optional. Avoid `null!` — prefer fixing the design so nullability is expressed in the type system.
- **Collections**: Prefer `IReadOnlyList<T>` on public interfaces — it communicates that callers shouldn't modify the collection. Avoid exposing `List<T>` or `T[]` directly.
- **Time**: Always `TimeProvider` (injected). Registered as `TimeProvider.System` singleton.
- **XML docs**: `/// <summary>` on all public and internal API surface.
- **NuGet**: Versions in `Directory.Packages.props` only, versionless `<PackageReference>` in `.csproj`.

## Entity Definition

New entities should extend `BaseEntity` (provides `Id`, `CreatedAt/By`, `UpdatedAt/By`, `IsDeleted`, `DeletedAt/By` — all set by `AuditingInterceptor` automatically) and use `BaseEntityRepository<T>` for data access. Aggregate roots can layer on top of `BaseEntity`.

Rules:
- Private setters, enforce invariants through methods
- Protected parameterless ctor for EF Core
- Derived entity ctor sets `Id = Guid.NewGuid()` (via `protected init`)
- Boolean naming: `Is*`/`Has*` in C#, prefix-free column names via `HasColumnName`
- Soft delete via `entity.SoftDelete()` / `entity.Restore()` — never set `IsDeleted` directly

## Audit Trail

`AuditEvent` is append-only, does NOT extend `BaseEntity`. No FK on `UserId` (users are hard-deleted). Fire-and-forget logging — failures never break operations.

```csharp
// Prefer AuditActions constants over raw strings for consistency and refactorability
await auditService.LogAsync(AuditActions.AdminAssignRole, userId: callerId,
    targetEntityType: "User", targetEntityId: targetId,
    metadata: JsonSerializer.Serialize(new { role = input.Role }), ct: cancellationToken);
```

Always serialize metadata with `JsonSerializer.Serialize` — never string interpolation (JSON injection risk).

## EF Core

Configurations inherit `BaseEntityConfiguration<T>` (`public abstract`), override `ConfigureEntity`. Mark derived configurations `internal`. Auto-discovered via `ApplyConfigurationsFromAssembly()`.

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

// Static message — prefer ErrorMessages constants for consistency and reuse
return Result.Failure(ErrorMessages.Admin.UserNotFound, ErrorType.NotFound);

// Runtime values go in server-side logs, never in client responses
logger.LogWarning("Operation failed for user '{UserId}': {Errors}", userId, errors);
return Result.Failure(ErrorMessages.Admin.DeleteFailed);
```

| ErrorType | HTTP | When |
|---|---|---|
| *(omit — default)* | 400 | Validation / business rule failures |
| `ErrorType.Unauthorized` | 401 | Auth / token failures |
| `ErrorType.Forbidden` | 403 | Authenticated but insufficient privileges |
| `ErrorType.NotFound` | 404 | Entity not found |

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
- Include `/// <summary>`, `[ProducesResponseType]` per status code, and `CancellationToken` as last param for complete OpenAPI docs and graceful cancellation
- Never `/// <param name="cancellationToken">` — it leaks into OAS `requestBody.description`
- File uploads: `[FromForm]` with `IFormFile`, `[Consumes("multipart/form-data")]`, `[RequestSizeLimit(bytes)]`
- Error responses: use `ProblemFactory.Create()` to ensure consistent RFC 9457 ProblemDetails format — avoid `NotFound()`, `BadRequest()`, or anonymous objects which produce inconsistent shapes
- Success responses: `Ok(response)`, `Created(string.Empty, response)` — `CreatedAtAction` is avoided since the Location header isn't used by the SvelteKit frontend
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

- Client-facing messages are centralized as `const string` in `ErrorMessages.cs` nested classes — reference constants rather than inline strings for consistency and single-source-of-truth
- Runtime values (role names, user IDs, framework errors): log server-side via `ILogger`, never in `Result.Failure()`
- Identity errors: log `.Description` server-side, return a static `ErrorMessages` constant to the client
- Exception: password validation errors (registration, change, reset) are forwarded as-is — they describe password policy, not internals

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

Custom repositories: extend `IBaseEntityRepository<T>` in Application, implement in Infrastructure with `BaseEntityRepository<T>`. Avoid exposing `IQueryable` across layer boundaries — it couples consumers to the EF Core query provider and makes testing harder.

Pagination: `Paginate(int pageNumber, int pageSize)` extension on `IQueryable<T>` returns `IQueryable<T>` (applies `Skip`/`Take`). Use in custom repository methods for list endpoints.

## Caching

`HybridCache` (.NET built-in) provides L1 in-process caching with stampede protection. Keys defined in `CacheKeys` constants. `UserCacheInvalidationInterceptor` auto-clears user cache on entity changes. `NoOpHybridCache` is registered when caching is disabled.

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

## Email Templates

Transactional emails use [Fluid](https://github.com/sebastienros/fluid) (Liquid) templates rendered by `IEmailTemplateRenderer`. Templates are embedded resources compiled once and cached for the application lifetime.

**Architecture:** `IEmailTemplateRenderer.Render<TModel>(templateName, model)` returns a `RenderedEmail` (subject, HTML body, optional plain text). Services inject `ITemplatedEmailSender` which wraps render + send in a try/catch (failures are logged but never propagated).

**3-file pattern** per email in `Infrastructure/Features/Email/Templates/`:
- `{name}.liquid` — HTML body fragment (inline styles, injected into `_base.liquid`)
- `{name}.subject.liquid` — Subject line (plain text)
- `{name}.text.liquid` — Plain text alternative (optional but recommended)

**Model records** in `Application/Features/Email/Models/EmailTemplateModels.cs` — one record per template. Properties auto-map to snake_case Liquid variables (e.g. `ResetUrl` becomes `reset_url`).

**Shared layout:** `_base.liquid` wraps all HTML emails with header (`{{ app_name }}`), card container, and footer. Fragments provide inner content only — the layout handles the outer HTML document.

**Security:** HTML body is rendered with `HtmlEncoder.Default` preventing XSS. Subject and plain text are unencoded. The `{{ body | raw }}` filter in `_base.liquid` safely injects pre-encoded child HTML.

**Adding a new template:** See [SKILLS.md — Add a Transactional Email Template](../../../SKILLS.md#add-a-transactional-email-template).

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

## Aspire (Local Development Orchestration)

**Two new projects** support .NET Aspire for local development:

### ServiceDefaults (`MyProject.ServiceDefaults`)

Shared Aspire project (`IsAspireSharedProject=true`) providing:
- **OpenTelemetry**: logging (formatted messages, scopes), metrics (ASP.NET Core, HTTP, Runtime), tracing (ASP.NET Core, HTTP with health-check filtering, EF Core), OTLP export
- **Service discovery**: automatic for `HttpClient` instances
- **HTTP resilience**: standard resilience handler on all `HttpClient` instances

Called via `builder.AddServiceDefaults()` in `Program.cs`. Degrades gracefully when not running under Aspire — OTLP export only activates when `OTEL_EXPORTER_OTLP_ENDPOINT` is set.

Health check endpoint mapping is **not** included — the application's existing `MapHealthCheckEndpoints()` handles `/health`, `/health/ready`, and `/health/live`.

### AppHost (`MyProject.AppHost`)

Aspire orchestrator for local development. Launches all infrastructure as containers:

| Resource | Aspire method | Connection mapping |
|---|---|---|
| PostgreSQL + PgAdmin | `AddPostgres().WithPgAdmin().AddDatabase("Database")` | Auto → `ConnectionStrings:Database` |
| MinIO | `AddMinioContainer("storage").WithDataVolume()` | Via `WithEnvironment("FileStorage__*", ...)` |
| MailPit | `AddMailPit("mailpit", httpPort, smtpPort)` | Via `WithEnvironment("Email__Smtp__*", ...)` |
| API | `AddProject<Projects.MyProject_WebApi>("api")` | References all above, port pinned by init |
| Frontend | `AddViteApp("frontend", ...).WithPnpm()` | Port pinned by init |

**Run**: `dotnet run --project src/backend/MyProject.AppHost`

### Aspire vs Docker Compose

| | Aspire (AppHost) | Docker Compose |
|---|---|---|
| Purpose | Local development | Production deployment |
| Dashboard | Built-in (OTEL traces, logs, metrics) | N/A (stdout logs, external collector) |
| Config | `WithEnvironment` in `Program.cs` | env files in `deploy/envs/` |
| Frontend | Hot reload via Vite dev server | Built static bundle |

### Logging: Serilog → OpenTelemetry

Serilog bridges to OpenTelemetry via the ServiceDefaults `AddOpenTelemetry()` logger provider. With `writeToProviders: true`, Serilog forwards logs to all registered `ILoggerProvider` instances including the OTEL one — no separate Serilog OTEL sink needed. When `OTEL_EXPORTER_OTLP_ENDPOINT` is set (by Aspire or production infrastructure), logs flow to the OTLP collector. The console sink always remains active.

### Adding a new infrastructure dependency to AppHost

1. Add hosting package to `Directory.Packages.props` and `MyProject.AppHost.csproj`
2. Add resource in `MyProject.AppHost/Program.cs` (e.g., `builder.AddRabbitMQ("rabbitmq")`)
3. Wire to API via `.WithReference()` or `.WithEnvironment()` depending on connection format
4. Add `.WaitFor()` if the API needs the resource at startup
