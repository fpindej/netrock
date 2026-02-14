using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MyProject.WebApi.Shared;

/// <summary>
/// Abstract base controller for all authorized, versioned API endpoints.
/// Provides <c>[ApiController]</c>, <c>[Authorize]</c>, and the <c>api/v1/[controller]</c> route prefix.
/// Error status codes map to <see cref="ProblemDetails"/> via ASP.NET Core's client error mapping.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public abstract class ApiController : ControllerBase;
