# Controller Action Template

```csharp
/// <summary>
/// {ActionDescription}.
/// </summary>
/// <returns>{ReturnDescription}</returns>
/// <response code="200">Returns the {entity}</response>
/// <response code="400">If the request is invalid</response>
/// <response code="401">If the user is not authenticated</response>
/// <response code="404">If the {entity} was not found</response>
[Http{Method}("{route}")]
[ProducesResponseType(typeof({Response}), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<ActionResult<{Response}>> {ActionName}(
    {Parameters},
    CancellationToken cancellationToken)
{
    var result = await {service}.{Method}Async({args}, cancellationToken);

    if (!result.IsSuccess)
    {
        return ProblemFactory.Create(result.Error, result.ErrorType);
    }

    return Ok(result.Value.ToResponse());
}
```

## Variants

**Create (POST):**
```csharp
[HttpPost]
[ProducesResponseType(typeof({Response}), StatusCodes.Status201Created)]
// ...
public async Task<ActionResult<{Response}>> Create{Entity}(
    [FromBody] {Request} request,
    CancellationToken cancellationToken)
{
    // ...
    return Created(string.Empty, result.Value.ToResponse());
}
```

**Update (PUT/PATCH):**
```csharp
[Http{Put|Patch}("{id:guid}")]
// ...
public async Task<ActionResult<{Response}>> Update{Entity}(
    Guid id,
    [FromBody] {Request} request,
    CancellationToken cancellationToken)
```

**Delete (DELETE):**
```csharp
[HttpDelete("{id:guid}")]
[ProducesResponseType(StatusCodes.Status204NoContent)]
// ...
public async Task<IActionResult> Delete{Entity}(
    Guid id,
    CancellationToken cancellationToken)
{
    // ...
    return NoContent();
}
```

**With permission:**
```csharp
[RequirePermission(AppPermissions.Feature.Action)]
```

## Rules

- `/// <summary>` on every action
- `[ProducesResponseType]` for all status codes (no `typeof` on error codes)
- `CancellationToken` as last parameter - never document with `/// <param>`
- Error responses via `ProblemFactory.Create()` - never `NotFound()` or `BadRequest()`
- Success: `Ok(response)` or `Created(string.Empty, response)` - no `CreatedAtAction`
