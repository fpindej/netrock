---
disable-model-invocation: true
---

Adds a route constraint for validating string path parameters at the routing layer.

## Steps

1. Create `src/backend/Test.WebApi/Routing/{Name}RouteConstraint.cs`:
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
4. Non-matching routes return 404 automatically - no controller code needed
5. Verify: `dotnet build src/backend/Test.slnx`
