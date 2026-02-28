# Skills — Operations Cookbook

Step-by-step recipes for common operations. Each recipe lists exact paths, patterns, and commands. Follow mechanically — no interpretation needed.

> **Breaking change?** Before modifying any existing file, check [FILEMAP.md](FILEMAP.md) for downstream impact. When a change affects public API surface (DTOs, endpoints, interfaces), prefer additive changes over modifications. If a breaking change is unavoidable, document it in the commit body and update all affected consumers in the same commit.

---

## Backend Skills

### Add a NuGet Package

1. Add version to `src/backend/Directory.Packages.props`:
   ```xml
   <PackageVersion Include="PackageName" Version="X.Y.Z" />
   ```
2. Add reference to the target `.csproj` **without** a version:
   ```xml
   <PackageReference Include="PackageName" />
   ```
3. Verify: `dotnet build src/backend/MyProject.slnx`

### Add an Error Message

1. Open `src/backend/MyProject.Shared/ErrorMessages.cs`
2. Add `const string` to the appropriate nested class (or create a new one):
   ```csharp
   public static class Orders
   {
       public const string NotFound = "Order not found.";
   }
   ```
3. Use in service: `Result.Failure(ErrorMessages.Orders.NotFound)`
4. For dynamic messages, use inline interpolation: `$"Order '{orderNumber}' not found."`

### Add an Entity (End-to-End)

**Domain layer:**

1. Create `src/backend/MyProject.Domain/Entities/{Entity}.cs`:
   ```csharp
   public class Order : BaseEntity
   {
       public string Name { get; private set; } = string.Empty;
       protected Order() { }
       public Order(string name) { Id = Guid.NewGuid(); Name = name; }
   }
   ```
2. If enums needed, create alongside entity with explicit integer values
3. Add error messages to `ErrorMessages.cs`

**Application layer:**

4. Create `src/backend/MyProject.Application/Features/{Feature}/I{Feature}Service.cs`
5. Create DTOs in `src/backend/MyProject.Application/Features/{Feature}/Dtos/`:
   - `{Operation}Input.cs` (records)
   - `{Entity}Output.cs` (records)
6. *(Optional)* If custom queries needed: create `src/backend/MyProject.Application/Features/{Feature}/Persistence/I{Feature}Repository.cs` extending `IBaseEntityRepository<T>`

**Infrastructure layer:**

7. Create EF config in `src/backend/MyProject.Infrastructure/Features/{Feature}/Configurations/{Entity}Configuration.cs`:
   - Extend `BaseEntityConfiguration<T>`, override `ConfigureEntity`
   - Mark `internal`
   - Add `.HasComment()` on enum columns
8. Add `DbSet<Entity>` to `src/backend/MyProject.Infrastructure/Persistence/MyProjectDbContext.cs`
9. Create service in `src/backend/MyProject.Infrastructure/Features/{Feature}/Services/{Feature}Service.cs`:
   - Mark `internal`, use primary constructor
10. *(Optional)* If custom repository: create `src/backend/MyProject.Infrastructure/Features/{Feature}/Persistence/{Feature}Repository.cs` extending `BaseEntityRepository<T>`
11. Create DI extension in `src/backend/MyProject.Infrastructure/Features/{Feature}/Extensions/ServiceCollectionExtensions.cs`:
    - Use C# 13 `extension(IServiceCollection)` syntax

**WebApi layer:**

12. Create controller in `src/backend/MyProject.WebApi/Features/{Feature}/{Feature}Controller.cs`:
    - Extend `ApiController` (authenticated) or `ControllerBase` (public)
    - XML docs + `[ProducesResponseType]` on every action
13. Create request/response DTOs in `src/backend/MyProject.WebApi/Features/{Feature}/Dtos/{Operation}/`
14. Create mapper in `src/backend/MyProject.WebApi/Features/{Feature}/{Feature}Mapper.cs` (mark `internal`)
15. Create validators co-located with request DTOs
16. Wire DI in `src/backend/MyProject.WebApi/Program.cs`

**Migration:**

17. Run:
    ```bash
    dotnet ef migrations add Add{Entity} \
      --project src/backend/MyProject.Infrastructure \
      --startup-project src/backend/MyProject.WebApi \
      --output-dir Persistence/Migrations
    ```

**Tests:**

18. Add component test for the service in `src/backend/tests/MyProject.Component.Tests/Services/{Feature}ServiceTests.cs` (see [Add a Component Test](#add-a-component-test))
19. Add API integration tests in `src/backend/tests/MyProject.Api.Tests/Controllers/{Feature}ControllerTests.cs` (see [Add an API Integration Test](#add-an-api-integration-test))
20. Add validator tests in `src/backend/tests/MyProject.Api.Tests/Validators/{Validator}Tests.cs` (see [Add a Validator Test](#add-a-validator-test))

**Verify:** `dotnet test src/backend/MyProject.slnx -c Release`

**Commit strategy:** entity+config+errors → interface+DTOs → service+DI → controller+DTOs+mapper+validators → migration → tests

### Add an Endpoint to an Existing Feature

1. *(If new request/response needed)* Create DTOs in `WebApi/Features/{Feature}/Dtos/{Operation}/`
2. *(If new input/output needed)* Create DTOs in `Application/Features/{Feature}/Dtos/`
3. Add method to `Application/Features/{Feature}/I{Feature}Service.cs`
4. Implement in `Infrastructure/Features/{Feature}/Services/{Feature}Service.cs`
5. Add mapper methods to `WebApi/Features/{Feature}/{Feature}Mapper.cs`
6. Add controller action to `WebApi/Features/{Feature}/{Feature}Controller.cs`:
   - `/// <summary>` + `[ProducesResponseType]` + `CancellationToken`
7. Add validator if needed
8. Add/update component tests for the service method (see [Add a Component Test](#add-a-component-test))
9. Add/update API integration tests for the new action (see [Add an API Integration Test](#add-an-api-integration-test))
10. If validator added, add validator tests (see [Add a Validator Test](#add-a-validator-test))
11. Verify: `dotnet test src/backend/MyProject.slnx -c Release`
12. **After deploying/running:** regenerate frontend types (see [Regenerate API Types](#regenerate-api-types))

> **Breaking change check:** If modifying an existing endpoint's request/response shape, this is a breaking change for the frontend. Either version the endpoint or update the frontend in the same PR.

### Add an Options Class

1. Create in the appropriate layer:
   - Infrastructure: `src/backend/MyProject.Infrastructure/{Area}/Options/{Name}Options.cs`
   - WebApi: `src/backend/MyProject.WebApi/Options/{Name}Options.cs`
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
3. Add section to `src/backend/MyProject.WebApi/appsettings.json` (and `appsettings.Development.json` if dev differs — note: dev/test appsettings are excluded from production publish via `StripDevConfig`; see backend `AGENTS.md` → Hosting Configuration → Production build hygiene)
4. Register in DI extension:
   ```csharp
   services.AddOptions<{Name}Options>()
       .BindConfiguration({Name}Options.SectionName)
       .ValidateDataAnnotations()
       .ValidateOnStart();
   ```
5. Add env var to `deploy/envs/local/api.env` (and `deploy/envs/production-example/api.env`) if configurable at deploy time

### Run a Migration

```bash
dotnet ef migrations add {MigrationName} \
  --project src/backend/MyProject.Infrastructure \
  --startup-project src/backend/MyProject.WebApi \
  --output-dir Persistence/Migrations
```

To apply (development — runs automatically on startup, but can be run manually):

```bash
dotnet ef database update \
  --project src/backend/MyProject.Infrastructure \
  --startup-project src/backend/MyProject.WebApi
```

### Add a Role

1. Add `public const string` field to `src/backend/MyProject.Application/Identity/Constants/AppRoles.cs`
2. That's it — `AppRoles.All` discovers roles via reflection, seeding picks them up automatically
3. *(Optional)* To seed default permissions for the new role, add entries to `SeedRolePermissionsAsync()` in `src/backend/MyProject.Infrastructure/Persistence/Extensions/ApplicationBuilderExtensions.cs`

### Add a Permission

**Backend:**

1. Add `public const string` field to the appropriate nested class in `src/backend/MyProject.Application/Identity/Constants/AppPermissions.cs`:
   ```csharp
   public static class Orders
   {
       public const string View = "orders.view";
       public const string Manage = "orders.manage";
   }
   ```
   `AppPermissions.All` discovers permissions via reflection — no manual registration needed.
2. Add `[RequirePermission(AppPermissions.Orders.View)]` to the relevant controller actions
3. *(Optional)* Seed the permission for existing roles in `SeedRolePermissionsAsync()` in `src/backend/MyProject.Infrastructure/Persistence/Extensions/ApplicationBuilderExtensions.cs`
4. Verify: `dotnet build src/backend/MyProject.slnx`

**Frontend:**

5. Add matching constants to `src/frontend/src/lib/utils/permissions.ts`:
   ```typescript
   Orders: {
       View: 'orders.view',
       Manage: 'orders.manage',
   },
   ```
6. Use in components: `hasPermission(user, Permissions.Orders.View)`
7. If adding a new admin page: add a per-page guard in `+page.server.ts`:
   ```typescript
   if (!hasPermission(user, Permissions.Orders.View)) throw redirect(303, '/');
   ```
8. If adding a sidebar nav item: add `permission: Permissions.Orders.View` to the nav item in `SidebarNav.svelte` — items are filtered per-permission, not as a group
9. Verify: `cd src/frontend && pnpm run test && pnpm run format && pnpm run lint && pnpm run check`

### Add a Rate Limit Policy

1. Add a constant to `src/backend/MyProject.WebApi/Shared/RateLimitPolicies.cs`:
   ```csharp
   public const string MyPolicy = "my-policy";
   ```
2. Add a nested configuration class inside `RateLimitingOptions` in `src/backend/MyProject.WebApi/Options/RateLimitingOptions.cs` (extend `FixedWindowPolicyOptions`):
   ```csharp
   // Inside RateLimitingOptions class:
   public sealed class MyPolicyLimitOptions : FixedWindowPolicyOptions
   {
       public MyPolicyLimitOptions()
       {
           PermitLimit = 10;
           Window = TimeSpan.FromMinutes(1);
           QueueLimit = 0;
       }
   }
   ```
3. Add the property to `RateLimitingOptions` (same file):
   ```csharp
   [Required]
   [ValidateObjectMembers]
   public MyPolicyLimitOptions MyPolicy { get; init; } = new();
   ```
4. Register in `src/backend/MyProject.WebApi/Extensions/RateLimiterExtensions.cs` using the existing helpers:
   - `AddIpPolicy(...)` for unauthenticated endpoints (partitions by IP)
   - `AddUserPolicy(...)` for authenticated endpoints (partitions by user identity)
5. Add config section to both `appsettings.json` and `appsettings.Development.json`
6. Apply to endpoints: `[EnableRateLimiting(RateLimitPolicies.MyPolicy)]`
7. Add `[ProducesResponseType(StatusCodes.Status429TooManyRequests)]` to the endpoint
8. Verify: `dotnet build src/backend/MyProject.slnx`

### Add a Route Constraint

For validating string path parameters (e.g. `{role}`, `{jobId}`) at the routing layer.

1. Create `src/backend/MyProject.WebApi/Routing/{Name}RouteConstraint.cs`:
   ```csharp
   public partial class {Name}RouteConstraint : IRouteConstraint
   {
       public bool Match(HttpContext? httpContext, IRouter? route, string routeKey,
           RouteValueDictionary values, RouteDirection routeDirection)
       {
           if (!values.TryGetValue(routeKey, out var value) || value is not string s)
               return false;
           return s.Length <= 100 && Pattern().IsMatch(s);
       }

       [GeneratedRegex(@"^[A-Za-z0-9._-]+$")]
       private static partial Regex Pattern();
   }
   ```
2. Register in `Program.cs` inside `AddRouting`:
   ```csharp
   options.ConstraintMap.Add("myConstraint", typeof({Name}RouteConstraint));
   ```
3. Use in routes: `[HttpGet("items/{id:myConstraint}")]`
4. Non-matching routes return 404 automatically — no controller code needed
5. Verify: `dotnet build src/backend/MyProject.slnx`

### Add a Background Job

The template uses [Hangfire](https://www.hangfire.io/) for recurring background jobs with PostgreSQL persistence. Jobs implement the `IRecurringJobDefinition` interface and are auto-discovered at startup.

**1. Create the job class** in `src/backend/MyProject.Infrastructure/Features/Jobs/RecurringJobs/{JobName}Job.cs`:

```csharp
using Hangfire;
using Microsoft.Extensions.Logging;

namespace MyProject.Infrastructure.Features.Jobs.RecurringJobs;

/// <summary>
/// Brief description of what this job does and why.
/// </summary>
internal sealed class MyCleanupJob(
    MyProjectDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<MyCleanupJob> logger) : IRecurringJobDefinition
{
    /// <inheritdoc />
    public string JobId => "my-cleanup";

    /// <inheritdoc />
    public string CronExpression => Cron.Daily();

    /// <inheritdoc />
    public async Task ExecuteAsync()
    {
        // Job logic here — each execution gets its own DI scope
        logger.LogInformation("Job completed");
    }
}
```

Key conventions:
- Mark `internal sealed`
- Use primary constructor for DI (scoped services work — each execution gets its own scope)
- Use `TimeProvider` (never `DateTime.UtcNow`)
- Use descriptive `JobId` (kebab-case, e.g. `"expired-token-cleanup"`)
- Use `Hangfire.Cron` helpers: `Cron.Hourly()`, `Cron.Daily()`, `Cron.Weekly()`, etc.

**2. Register in DI** — add two lines to `src/backend/MyProject.Infrastructure/Features/Jobs/Extensions/ServiceCollectionExtensions.cs`:

```csharp
services.AddScoped<MyCleanupJob>();
services.AddScoped<IRecurringJobDefinition>(sp => sp.GetRequiredService<MyCleanupJob>());
```

**3. Verify:** `dotnet build src/backend/MyProject.slnx`

That's it — `UseJobScheduling()` discovers all `IRecurringJobDefinition` implementations and registers them with Hangfire automatically.

**Admin UI:** The job will appear in the admin panel at `/admin/jobs` (requires `jobs.view` permission). Users with `jobs.manage` can trigger, pause, resume, delete, and restore jobs. Pause state is persisted to the database (`hangfire.pausedjobs`) and survives restarts. The "Restore Jobs" button re-registers all job definitions without an app restart.

**Configuration:** Job scheduling can be toggled via `appsettings.json`:
```json
"JobScheduling": {
  "Enabled": true,
  "WorkerCount": 4
}
```
Set `Enabled` to `false` to disable Hangfire entirely (e.g. read-only replicas, specific deployment nodes).

**Dev dashboard:** In development, the built-in Hangfire dashboard is available at `http://localhost:8080/hangfire`.

### Fire a One-Time Background Job

For ad-hoc work that should run once in the background (send email, call external API, process file), use Hangfire's `IBackgroundJobClient` directly. No custom interface needed — any DI-registered service with a public method works.

**1. Create the job class** (or use any existing service) in `src/backend/MyProject.Infrastructure/Features/Jobs/`:

```csharp
using Microsoft.Extensions.Logging;

namespace MyProject.Infrastructure.Features.Jobs;

internal sealed class WelcomeEmailJob(
    ITemplatedEmailSender templatedEmailSender,
    ILogger<WelcomeEmailJob> logger)
{
    public async Task ExecuteAsync(string userId, string email)
    {
        await templatedEmailSender.SendSafeAsync("welcome", new WelcomeModel(email), email, default);
        logger.LogInformation("Sent welcome email to user '{UserId}'", userId);
    }
}
```

Key conventions:
- All parameters must be **JSON-serializable** (strings, numbers, DTOs) — Hangfire persists them to the database
- Never pass `IServiceProvider`, `HttpContext`, `DbContext`, or other non-serializable objects as arguments
- Hangfire creates a fresh DI scope per execution, so scoped services (like `DbContext`) are safe to inject via constructor

**2. Register in DI** — add to `ServiceCollectionExtensions.cs`:

```csharp
services.AddScoped<WelcomeEmailJob>();
```

**3. Enqueue from any service or controller** — inject `IBackgroundJobClient`:

```csharp
// Fire-and-forget (runs immediately in background)
backgroundJobClient.Enqueue<WelcomeEmailJob>(
    job => job.ExecuteAsync(user.Id, user.Email));

// Delayed (runs after a time span)
backgroundJobClient.Schedule<WelcomeEmailJob>(
    job => job.ExecuteAsync(user.Id, user.Email),
    TimeSpan.FromMinutes(30));
```

**4. Verify:** `dotnet build src/backend/MyProject.slnx`

See `ExampleFireAndForgetJob.cs` in the codebase for a working reference. The Hangfire dashboard and admin UI at `/admin/jobs` show one-time job executions alongside recurring jobs.

### Use File Storage (Upload / Download / Delete)

`IFileStorageService` provides generic S3-compatible file operations. Inject it in any service:

```csharp
// Upload
var result = await fileStorageService.UploadAsync("documents/123.pdf", data, "application/pdf", ct);

// Download
var result = await fileStorageService.DownloadAsync("documents/123.pdf", ct);
if (result.IsSuccess) { var bytes = result.Value.Data; var mime = result.Value.ContentType; }

// Delete (best-effort pattern — log warning, don't block)
var deleteResult = await fileStorageService.DeleteAsync("documents/123.pdf", ct);
if (!deleteResult.IsSuccess) logger.LogWarning("...");

// Check existence
var exists = await fileStorageService.ExistsAsync("documents/123.pdf", ct);
```

**Storage key convention:** `{feature}/{id}.{ext}` (e.g., `avatars/{userId}.webp`). Overwriting the same key replaces the file.

### Swap S3 Provider (Cloudflare R2, DigitalOcean Spaces, etc.)

No code changes needed — `S3FileStorageService` uses the standard S3 API. Only configuration changes:

**1. Update `deploy/envs/production-example/compose.env`** (or your actual env):

```env
# Cloudflare R2
FileStorage__Endpoint=https://<account-id>.r2.cloudflarestorage.com
FileStorage__AccessKey=<R2-access-key>
FileStorage__SecretKey=<R2-secret-key>
FileStorage__BucketName=my-bucket
FileStorage__UseSSL=true
FileStorage__Region=auto

# DigitalOcean Spaces
FileStorage__Endpoint=https://<region>.digitaloceanspaces.com
FileStorage__AccessKey=<spaces-key>
FileStorage__SecretKey=<spaces-secret>
FileStorage__BucketName=my-space
FileStorage__UseSSL=true
FileStorage__Region=<region>

# Backblaze B2 (S3-compatible)
FileStorage__Endpoint=https://s3.<region>.backblazeb2.com
FileStorage__AccessKey=<b2-key-id>
FileStorage__SecretKey=<b2-application-key>
FileStorage__BucketName=my-bucket
FileStorage__UseSSL=true
FileStorage__Region=<region>
```

**2. Pre-create the bucket** in your provider's console (auto-creation requires elevated permissions).

**3. Restart** — no code changes, no rebuild.

### Remove File Storage Entirely

If you don't need file uploads:

1. **Docker:** Remove `storage` service from `docker-compose.yml`, `docker-compose.local.yml`, `docker-compose.production.yml`
2. **Backend:** Remove `Application/Features/FileStorage/`, `Application/Features/Avatar/`, `Infrastructure/Features/FileStorage/`, `Infrastructure/Features/Avatar/`
3. **Entity:** Remove `HasAvatar` from `ApplicationUser`
4. **Endpoints:** Remove avatar endpoints from `UsersController`, `UploadAvatar/` DTOs
5. **DTOs:** Remove `HasAvatar` from `UserOutput`, `AdminUserOutput`, `UserResponse`, `AdminUserResponse`, mappers
6. **DI:** Remove `AddFileStorageServices()` and `AddAvatarServices()` from `Program.cs`
7. **NuGet:** Remove `AWSSDK.S3`, `SkiaSharp`, `SkiaSharp.NativeAssets.Linux.NoDependencies` from `Directory.Packages.props`
8. **Frontend:** Remove `AvatarDialog.svelte`, update `ProfileHeader.svelte` and `UserNav.svelte` to remove avatar image
9. **Config:** Remove `FileStorage` section from all `appsettings*.json` and env files
10. **Health check:** Remove S3 health check from `HealthCheckExtensions.cs`

### Add a Transactional Email Template

The template uses [Fluid](https://github.com/sebastienros/fluid) (Liquid) for rendering email templates. Each email has a 3-file pattern plus a model record.

**1. Create the model record** in `src/backend/MyProject.Application/Features/Email/Models/EmailTemplateModels.cs`:

```csharp
/// <summary>
/// Model for the order-confirmation template. Exposed as <c>order_number</c> and <c>total</c> in Liquid.
/// </summary>
/// <param name="OrderNumber">The order number.</param>
/// <param name="Total">The formatted order total (e.g. "$42.00").</param>
public record OrderConfirmationModel(string OrderNumber, string Total);
```

Properties are automatically exposed as snake_case Liquid variables (e.g. `OrderNumber` becomes `order_number`).

**2. Create the 3 template files** in `src/backend/MyProject.Infrastructure/Features/Email/Templates/`:

- `order-confirmation.liquid` — HTML body fragment (injected into `_base.liquid` via `{{ body | raw }}`):
  ```html
  <h2 style="margin: 0 0 20px 0; font-size: 22px; font-weight: 700; color: #333333;">Order Confirmed</h2>
  <p style="margin: 0 0 16px 0;">Your order <strong>{{ order_number }}</strong> totaling {{ total }} has been confirmed.</p>
  ```
- `order-confirmation.subject.liquid` — Subject line (plain text, no HTML):
  ```
  Order {{ order_number }} Confirmed
  ```
- `order-confirmation.text.liquid` — Plain text body (optional but recommended):
  ```
  Order Confirmed

  Your order {{ order_number }} totaling {{ total }} has been confirmed.
  ```

All three files must be marked as **EmbeddedResource** — verify the `.csproj` has `<EmbeddedResource Include="Features\Email\Templates\*.liquid" />` (already present).

**3. Register the model type** in `FluidEmailTemplateRenderer.CreateOptions()` in `src/backend/MyProject.Infrastructure/Features/Email/Services/FluidEmailTemplateRenderer.cs`:

```csharp
options.MemberAccessStrategy.Register<OrderConfirmationModel>();
```

**4. Use in a service** — inject `ITemplatedEmailSender`:

```csharp
await templatedEmailSender.SendSafeAsync("order-confirmation",
    new OrderConfirmationModel(order.Number, order.FormattedTotal),
    customer.Email, cancellationToken);
```

**5. Add tests** in `src/backend/tests/MyProject.Component.Tests/Services/FluidEmailTemplateRendererTests.cs` — at minimum test subject rendering, HTML variable injection, and plain text variant.

**6. Verify:** `dotnet test src/backend/MyProject.slnx -c Release`

Key conventions:
- Template names use kebab-case: `order-confirmation`, not `orderConfirmation`
- HTML templates use inline styles (email clients ignore `<style>` blocks in fragments)
- The shared base layout (`_base.liquid`) provides the HTML wrapper, header with `{{ app_name }}`, card container, and footer — fragments only provide inner content
- HTML body is rendered with `HtmlEncoder.Default` (XSS-safe); subject and plain text are unencoded
- Subject templates should use static text or config-sourced variables only — never raw user input (no HTML encoding applied, CRLF could cause header injection in poorly-configured SMTP libraries)
- `{{ app_name }}` is automatically available in all templates from `EmailOptions.FromName`

### Run Tests

```bash
# All tests (Release mode — matches CI)
dotnet test src/backend/MyProject.slnx -c Release

# Single project
dotnet test src/backend/tests/MyProject.Unit.Tests -c Release

# Filter by class name
dotnet test src/backend/tests/MyProject.Component.Tests -c Release --filter "FullyQualifiedName~AuthenticationServiceTests"

# Filter by method name
dotnet test src/backend/tests/MyProject.Unit.Tests -c Release --filter "ResultTests.Success_ReturnsIsSuccessTrue"
```

No external dependencies (Docker, PostgreSQL) needed — all tests run in-process.

### Add a Unit Test

For pure logic in Shared, Domain, or Application layers.

1. Create `src/backend/tests/MyProject.Unit.Tests/{Layer}/{ClassUnderTest}Tests.cs`
2. Structure:
   ```csharp
   namespace MyProject.Unit.Tests.{Layer};

   public class {ClassUnderTest}Tests
   {
       [Fact]
       public void {Method}_{Scenario}_{Expected}()
       {
           // Arrange / Act / Assert
       }
   }
   ```
3. No mocking, no DI — test pure inputs and outputs
4. Verify: `dotnet test src/backend/tests/MyProject.Unit.Tests -c Release`

### Add a Component Test

For service business logic with mocked dependencies.

1. Create `src/backend/tests/MyProject.Component.Tests/Services/{Service}Tests.cs`
2. Create mocks in constructor or setup method:
   ```csharp
   public class OrderServiceTests
   {
       private readonly MyProjectDbContext _dbContext = TestDbContextFactory.Create();
       private readonly IOrderRepository _orderRepo = Substitute.For<IOrderRepository>();
       private readonly HybridCache _cache = Substitute.For<HybridCache>();
       // ... create service instance with mocks
   }
   ```
3. For Identity mocking, use `IdentityMockHelpers`:
   ```csharp
   var userManager = IdentityMockHelpers.CreateMockUserManager();
   var roleManager = IdentityMockHelpers.CreateMockRoleManager();
   ```
4. Assert on `Result` properties:
   ```csharp
   Assert.True(result.IsSuccess);
   Assert.Equal(expectedId, result.Value);
   ```
5. Verify: `dotnet test src/backend/tests/MyProject.Component.Tests -c Release`

### Add an API Integration Test

For testing the full HTTP pipeline (routes, auth, validation, status codes).

1. Create `src/backend/tests/MyProject.Api.Tests/Controllers/{Controller}Tests.cs`
2. Structure:
   ```csharp
   public class OrdersControllerTests : IClassFixture<CustomWebApplicationFactory>, IDisposable
   {
       private readonly CustomWebApplicationFactory _factory;
       private readonly HttpClient _client;

       public OrdersControllerTests(CustomWebApplicationFactory factory)
       {
           _factory = factory;
           _client = factory.CreateClient();
       }

       public void Dispose() => _client.Dispose();
   }
   ```
3. Auth is controlled per-request via the `Authorization` header (parsed by `TestAuthHandler`):
   ```csharp
   // Basic authenticated user (User role, no permissions)
   new HttpRequestMessage(HttpMethod.Get, "/api/v1/endpoint")
       { Headers = { { "Authorization", "Test" } } };

   // User with specific permissions
   new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/users")
       { Headers = { { "Authorization", TestAuth.WithPermissions(AppPermissions.Users.View) } } };

   // SuperAdmin (bypasses all permission checks)
   new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/users")
       { Headers = { { "Authorization", TestAuth.SuperAdmin() } } };

   // Unauthenticated (no Authorization header — returns 401)
   using var anonClient = _factory.CreateClient();
   ```
4. Configure mock returns per test:
   ```csharp
   _factory.AdminService.GetUsersAsync(1, 10, null, Arg.Any<CancellationToken>())
       .Returns(new AdminUserListOutput([], 0, 1, 10));
   ```
5. If the service interface isn't already mocked in `CustomWebApplicationFactory`, add it there first
6. For success responses (200/201 with a body), add **response contract assertions**:
   - Add a frozen contract record to `tests/MyProject.Api.Tests/Contracts/ResponseContracts.cs` matching the response shape
   - Deserialize with `ReadFromJsonAsync<ContractRecord>()` and assert key fields are populated
   - This catches silent field renames, nullability changes, and removed properties
   ```csharp
   var body = await response.Content.ReadFromJsonAsync<MyContractRecord>();
   Assert.NotNull(body);
   Assert.NotEqual(Guid.Empty, body.Id);
   ```
7. Verify: `dotnet test src/backend/tests/MyProject.Api.Tests -c Release`

### Add a Validator Test

For testing FluentValidation rules without starting the test server. Uses FluentValidation's `TestHelper` extensions.

1. Create `src/backend/tests/MyProject.Api.Tests/Validators/{Validator}Tests.cs`
2. Instantiate the validator directly and use `TestValidate` + assertion helpers:
   ```csharp
   using FluentValidation.TestHelper;

   public class OrderRequestValidatorTests
   {
       private readonly OrderRequestValidator _validator = new();

       [Fact]
       public void ValidRequest_ShouldPassValidation()
       {
           var result = _validator.TestValidate(new OrderRequest { Name = "Widget", Quantity = 1 });

           result.ShouldNotHaveAnyValidationErrors();
       }

       [Fact]
       public void MissingName_ShouldFail()
       {
           var result = _validator.TestValidate(new OrderRequest { Name = "", Quantity = 1 });

           result.ShouldHaveValidationErrorFor(x => x.Name);
       }

       [Fact]
       public void InvalidQuantity_ShouldFailWithMessage()
       {
           var result = _validator.TestValidate(new OrderRequest { Name = "Widget", Quantity = -1 });

           result.ShouldHaveValidationErrorFor(x => x.Quantity)
               .WithErrorMessage("Quantity must be positive.");
       }
   }
   ```
3. Verify: `dotnet test src/backend/tests/MyProject.Api.Tests -c Release`

---

## Frontend Skills

### Run Frontend Tests

```bash
# All tests (matches CI)
cd src/frontend && pnpm run test

# Filter by file name
cd src/frontend && pnpm run test -- src/lib/auth/middleware.test.ts

# Filter by test name
cd src/frontend && pnpm run test -- -t "refreshes token"

# Watch mode (re-runs on file changes)
cd src/frontend && pnpm run test:watch
```

No external dependencies needed — tests run in-process with vitest.

### Regenerate API Types

**Prerequisite:** Backend must be running (Docker or IDE).

```bash
cd src/frontend && pnpm run api:generate
```

This updates `src/frontend/src/lib/api/v1.d.ts`. After regenerating:

1. Review changes in `v1.d.ts` for breaking changes
2. Update any affected API calls or type aliases in `src/frontend/src/lib/types/index.ts`
3. Run `cd src/frontend && pnpm run check` to catch type errors
4. Commit `v1.d.ts` with the changes that caused the regeneration

### Add a Page

1. Create route directory: `src/frontend/src/routes/(app)/{feature}/`
2. Create `+page.svelte`:
   ```svelte
   <script lang="ts">
       import * as m from '$lib/paraglide/messages';
   </script>

   <svelte:head>
       <title>{m.meta_{feature}_title()}</title>
   </svelte:head>
   ```
3. *(If server data needed)* Create `+page.server.ts`:
   ```typescript
   import { createApiClient } from '$lib/api';
   import type { PageServerLoad } from './$types';

   export const load: PageServerLoad = async ({ fetch, url }) => {
       const client = createApiClient(fetch, url.origin);
       const { data } = await client.GET('/api/v1/...');
       return { ... };
   };
   ```
4. Add i18n keys to `src/frontend/src/messages/en.json` and `cs.json`
5. Add navigation entry in `src/frontend/src/lib/components/layout/SidebarNav.svelte`
6. Verify: `cd src/frontend && pnpm run test && pnpm run format && pnpm run lint && pnpm run check`

### Add a Component

1. Create feature folder: `src/frontend/src/lib/components/{feature}/`
2. Create component: `{Name}.svelte` with `interface Props` + `$props()`
3. Create barrel: `src/frontend/src/lib/components/{feature}/index.ts`:
   ```typescript
   export { default as {Name} } from './{Name}.svelte';
   ```
4. Import via barrel: `import { {Name} } from '$lib/components/{feature}';`

### Add i18n Keys

1. Add to `src/frontend/src/messages/en.json`:
   ```json
   { "{domain}_{feature}_{element}": "English text" }
   ```
2. Add to `src/frontend/src/messages/cs.json`:
   ```json
   { "{domain}_{feature}_{element}": "Czech text" }
   ```
3. Use: `import * as m from '$lib/paraglide/messages'; m.{domain}_{feature}_{element}()`

### Add a shadcn Component

```bash
cd src/frontend && pnpm dlx shadcn-svelte@latest add {component-name}
```

Generates in `src/frontend/src/lib/components/ui/{component}/`. After adding:

1. Convert any physical CSS to logical (`ml-*` → `ms-*`, etc.)
2. Available: alert, avatar, badge, button, card, checkbox, dialog, dropdown-menu, input, label, phone-input, select, separator, sheet, sonner, textarea, timeline, tooltip
3. Browse full catalog: [ui.shadcn.com](https://ui.shadcn.com)

### Add a Package

1. `cd src/frontend && pnpm add {package}`
2. For dev dependencies: `pnpm add -D {package}`
3. Verify: `pnpm run check`

### Style & Responsive Design Pass

Recipe for reviewing or improving a page's styling, responsiveness, and UX.

**1. Audit the page at key viewports:**

Open the page and check at these widths: **320px**, **375px**, **768px**, **1024px**, **1440px**.

**2. Check these rules (see `src/frontend/AGENTS.md` Styling section):**

| Rule | Check |
|---|---|
| Logical CSS only | No `ml-*`/`mr-*`/`pl-*`/`pr-*` — use `ms-*`/`me-*`/`ps-*`/`pe-*` |
| Mobile-first | Base styles for 320px, then `sm:`, `md:`, `lg:`, `xl:` for larger |
| Touch targets | Interactive elements ≥ 40px (`h-10`), primary actions ≥ 44px (`h-11`) |
| Font sizes | Minimum `text-xs` (12px) — never `text-[10px]` or smaller |
| Responsive padding | Scale with breakpoints (`p-4 sm:p-6 lg:p-8`) — no flat large padding |
| Grid in dialogs | Start with `grid-cols-1` base, add responsive breakpoints for multi-column |
| Sidebar-aware grids | Use `xl:grid-cols-2` for content grids — not `lg:` (sidebar takes ~250px) |
| Full-height layouts | `h-dvh` not `h-screen` (accounts for mobile browser chrome) |
| Flex overflow | `min-w-0` on flex children with text, `truncate`/`overflow-hidden` where needed |
| Non-shrinking elements | `shrink-0` on icons, badges, buttons alongside text |
| Reduced motion | `motion-safe:` prefix on animations, `prefers-reduced-motion` media query |
| No max-width on cards | Cards inside app layout fill their container — no `max-w-2xl` |
| `gap-*` over `space-x-*` | On flex/grid containers, use `gap-*` (direction-agnostic) |

**3. Apply existing page layout patterns:**

| Page type | Layout |
|---|---|
| Info + actions (2-col) | `grid gap-6 xl:grid-cols-2` |
| Single-column forms | `space-y-8` (no max-width) |
| Table + search | Full-width table, search bar above |

**4. Use the design system:**

- Colors: Use CSS variables from `src/frontend/src/styles/themes.css` via Tailwind tokens in `tailwind.css`
- shadcn components: Check [ui.shadcn.com](https://ui.shadcn.com) before building custom UI
- Class merging: Use `cn()` from `$lib/utils` for conditional classes — it handles Tailwind class conflicts correctly
- Animations: Define in `src/frontend/src/styles/animations.css`, use `motion-safe:` prefix

**5. Adding a theme variable:**

1. Define in `src/frontend/src/styles/themes.css` (`:root` + `.dark`)
2. Map in `src/frontend/src/styles/tailwind.css` (`@theme inline`)
3. Use in components: `bg-{variable}`, `text-{variable}-foreground`

**6. Verify:**

```bash
cd src/frontend && pnpm run test && pnpm run format && pnpm run lint && pnpm run check
```

---

## Full-Stack Skills

### Add an Environment Variable

**Backend-consumed variable:**

1. Add to `deploy/envs/local/api.env` with a working default value and comment (and `deploy/envs/production-example/api.env` with a placeholder). If the variable is consumed by Docker Compose interpolation (shared across services), use `compose.env` instead
2. If it maps to an Options class: use `Section__Key` naming (e.g., `Authentication__Jwt__AccessTokenLifetime=01:40:00`)
3. If it needs Docker wiring: add to `deploy/docker-compose.yml` (or the appropriate overlay) `environment` block with `${VAR}` interpolation, and place the variable in `compose.env`
4. If it needs an Options class: follow [Add an Options Class](#add-an-options-class)

**Frontend-consumed variable:**

1. Add to `src/frontend/.env.example` (documentation with placeholder)
2. Add to `src/frontend/.env.test` (valid test value for CI)
3. Add to `src/frontend/src/lib/config/server.ts` (server-only) or `i18n.ts` (client-safe)
4. Never export server config from the barrel (`$lib/config/index.ts`)

**Frontend `PUBLIC_*` variable (SvelteKit `$env/static/public`):**

1. Steps 1–2 above
2. Add `ARG` + `ENV` to `src/frontend/Dockerfile` (before `pnpm run build`)
3. Add `--build-arg` to `deploy/build.sh`, `deploy/build.ps1`, and `.github/workflows/docker.yml`
4. Add to the `frontend` service `environment` block in `deploy/docker-compose.yml` (or the appropriate overlay)
5. Import in components: `import { PUBLIC_VAR } from '$env/static/public';`

> **Note:** For secrets or keys that differ per environment (like Turnstile site keys), prefer runtime configuration via `$env/dynamic/private` with SSR layout data instead of build-time `PUBLIC_*` args. This avoids rebuilding images per environment.

### Add a Full-Stack Feature

Combines backend entity creation with frontend page. Follow in order:

**Backend (see [Add an Entity](#add-an-entity-end-to-end)):**

1. Domain: entity + enums + error messages
2. Application: interface + DTOs + (optional) repository interface
3. Infrastructure: EF config + DbSet + service + (optional) repository + DI extension
4. WebApi: controller + DTOs + mapper + validators + Program.cs wiring
5. Migration
6. Verify: `dotnet build src/backend/MyProject.slnx`

**Frontend (with backend running):**

7. Regenerate types: `cd src/frontend && pnpm run api:generate`
8. Add type alias to `src/frontend/src/lib/types/index.ts`
9. Create components in `src/frontend/src/lib/components/{feature}/` with barrel
10. Create page in `src/frontend/src/routes/(app)/{feature}/`
11. Add i18n keys to both `en.json` and `cs.json`
12. Update sidebar navigation
13. Verify: `cd src/frontend && pnpm run test && pnpm run format && pnpm run lint && pnpm run check`

**Commit strategy:** backend entity → backend service → backend controller → migration → frontend types+components → frontend page+i18n+nav

### Add a Project Area to CI

When adding a new frontend (React Native, Swift, etc.) or other project area that needs CI checks:

1. Open `.github/workflows/ci.yml`
2. Add a path filter in the `changes` job:
   ```yaml
   mobile:
     - 'src/mobile/**'
   ```
3. Add an output to the `changes` job:
   ```yaml
   mobile: ${{ steps.filter.outputs.mobile }}
   ```
4. Add a new job (copy an existing one and adapt):
   ```yaml
   mobile-checks:
     name: Mobile checks
     needs: changes
     if: needs.changes.outputs.mobile == 'true'
     runs-on: ubuntu-latest
     timeout-minutes: 10
     defaults:
       run:
         working-directory: src/mobile
     steps:
       - uses: actions/checkout@v6
       # ... setup + build + lint + test steps
   ```
5. Add the new job to the gate job's `needs`:
   ```yaml
   ci-passed:
     needs: [backend-build, frontend-checks, mobile-checks]
   ```
6. *(If the project has a Dockerfile)* Add a corresponding job to `.github/workflows/docker.yml` and update its `on.paths` and `dorny/paths-filter` filters
7. Verify: push a branch and confirm the new job appears in the PR checks

No branch protection changes needed — the `CI passed` gate job covers all upstream jobs automatically.

---

## Breaking Change Guidelines

When modifying existing code (not creating new), follow these rules:

### What Counts as a Breaking Change

| Layer | Breaking change |
|---|---|
| **Domain entity** | Renaming/removing a property, changing a type |
| **Application interface** | Changing a method signature, renaming/removing a method |
| **Application DTO** | Renaming/removing a field, changing nullability |
| **WebApi endpoint** | Changing route, method, request/response shape, status codes |
| **WebApi response DTO** | Renaming/removing a property, changing type or nullability |
| **Frontend API types** | Always regenerated — broken by any backend DTO change |
| **i18n keys** | Renaming a key (all usages break) |

### Safe Strategies

1. **Prefer additive changes** — add new fields/endpoints rather than removing or renaming existing ones, which break consumers
2. **Same-PR migration** — if a breaking change is needed, update all consumers (including frontend types) in the same PR
3. **V2 endpoint** — for significant endpoint changes, create a new versioned endpoint alongside the old one:
   - New route: `api/v2/{feature}/{action}`
   - Keep `api/v1/` working until all consumers migrate
   - Document deprecation in the commit body
4. **Deprecate then remove** — mark old code as obsolete in one PR, remove in a follow-up after confirming nothing depends on it

### Pre-Modification Checklist

Before changing any existing interface, DTO, or endpoint:

1. Check [FILEMAP.md](FILEMAP.md) for impact
2. Search for all usages: `grep -r "InterfaceName\|MethodName" src/`
3. If the change affects the OpenAPI spec → frontend types are affected → regenerate and fix
4. If the change affects i18n keys → update all `.json` message files and all component usages
5. Document the breaking change in the commit body
