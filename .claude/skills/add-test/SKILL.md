---
disable-model-invocation: true
argument-hint: "[unit|component|api|validator|frontend-unit|frontend-component]"
---

Adds tests. Specify test type or infer from context.

## Unit Test

For pure logic in Shared, Domain, or Application layers.

1. Create `src/backend/tests/MyProject.Unit.Tests/{Layer}/{ClassUnderTest}Tests.cs`
2. Structure: `{Method}_{Scenario}_{Expected}` naming
3. No mocking, no DI - test pure inputs and outputs
4. Verify: `dotnet test src/backend/tests/MyProject.Unit.Tests -c Release`

## Component Test

For service business logic with mocked dependencies.

1. Create `src/backend/tests/MyProject.Component.Tests/Services/{Service}Tests.cs`
2. Create mocks in constructor:
   ```csharp
   private readonly MyProjectDbContext _dbContext = TestDbContextFactory.Create();
   private readonly IOrderRepository _orderRepo = Substitute.For<IOrderRepository>();
   private readonly HybridCache _cache = Substitute.For<HybridCache>();
   ```
3. For Identity: `IdentityMockHelpers.CreateMockUserManager()` / `CreateMockRoleManager()`
4. Assert on `Result`: `Assert.True(result.IsSuccess); Assert.Equal(expectedId, result.Value);`
5. Verify: `dotnet test src/backend/tests/MyProject.Component.Tests -c Release`

## API Integration Test

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
3. Auth per-request via `Authorization` header:
   - `"Test"` - basic authenticated user
   - `TestAuth.WithPermissions(AppPermissions.Users.View)` - specific permissions
   - `TestAuth.Superuser()` - bypasses all checks
   - No header - unauthenticated (401)
4. Configure mock returns: `_factory.AdminService.Method(...).Returns(...);`
5. If service interface not mocked in `CustomWebApplicationFactory`, add it there first
6. For 200/201 responses, add **response contract assertions** using frozen records in `Contracts/ResponseContracts.cs`
7. Verify: `dotnet test src/backend/tests/MyProject.Api.Tests -c Release`

## Validator Test

For FluentValidation rules without starting the test server.

1. Create `src/backend/tests/MyProject.Api.Tests/Validators/{Validator}Tests.cs`
2. Use `TestValidate` + assertion helpers:
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
   }
   ```
3. Verify: `dotnet test src/backend/tests/MyProject.Api.Tests -c Release`

## Frontend Unit Test

For utility functions, state modules, and pure logic in `$lib/`.

1. Create `src/frontend/src/lib/{module}/{file}.test.ts` (co-located with source)
2. Import explicitly: `import { describe, it, expect, vi } from 'vitest'`
3. Default environment is `node`. For DOM tests, add `// @vitest-environment jsdom` at top of file
4. `restoreMocks: true` is configured globally - no manual cleanup needed
5. Verify: `cd src/frontend && pnpm run test`

## Frontend Component Test

For Svelte components with DOM interactions.

1. Create `src/frontend/src/lib/components/{feature}/{Component}.test.ts` (co-located)
2. Add `// @vitest-environment jsdom` at top of file
3. Mock SvelteKit modules as needed:
   ```typescript
   vi.mock('$app/navigation', () => ({ goto: vi.fn(), invalidateAll: vi.fn() }));
   vi.mock('$lib/paraglide/messages', () => new Proxy({}, { get: (_, key) => () => String(key) }));
   ```
4. Verify: `cd src/frontend && pnpm run test`
