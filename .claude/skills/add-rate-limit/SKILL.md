---
description: Add a rate limit policy to an endpoint
user_invocable: true
---

Adds a rate limit policy.

## Steps

1. Add a constant to `src/backend/MyProject.WebApi/Shared/RateLimitPolicies.cs`:
   ```csharp
   public const string MyPolicy = "my-policy";
   ```
2. Add a nested configuration class inside `RateLimitingOptions` in `src/backend/MyProject.WebApi/Options/RateLimitingOptions.cs` (extend `FixedWindowPolicyOptions`):
   ```csharp
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
4. Register in `src/backend/MyProject.WebApi/Extensions/RateLimiterExtensions.cs` using existing helpers:
   - `AddIpPolicy(...)` for unauthenticated endpoints (partitions by IP)
   - `AddUserPolicy(...)` for authenticated endpoints (partitions by user identity)
5. Add config section to both `appsettings.json` and `appsettings.Development.json`
6. Apply to endpoints: `[EnableRateLimiting(RateLimitPolicies.MyPolicy)]`
7. Add `[ProducesResponseType(StatusCodes.Status429TooManyRequests)]` to the endpoint
8. Verify: `dotnet build src/backend/MyProject.slnx`
