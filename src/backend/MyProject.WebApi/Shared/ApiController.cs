using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MyProject.WebApi.Shared;

/// <summary>
/// Abstract base controller for all authorized, versioned API endpoints.
/// Provides <c>[ApiController]</c>, <c>[Authorize]</c>, and the <c>api/v1/[controller]</c> route prefix.
/// All error status codes default to <see cref="ErrorResponse"/> via <see cref="ProducesErrorResponseTypeAttribute"/>.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[ProducesErrorResponseType(typeof(ErrorResponse))]
public abstract class ApiController : ControllerBase;
