using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MyProject.WebApi.Shared;

/// <summary>
/// Abstract base controller for all authorized API endpoints.
/// Provides <c>[ApiController]</c>, <c>[Authorize]</c>, and the <c>api/[controller]</c> route prefix.
/// <para>
/// Controllers that require authentication should extend this class to inherit the default
/// <c>[Authorize]</c> attribute and consistent route prefix. Public controllers (e.g. auth)
/// should extend <see cref="ControllerBase"/> directly with their own route.
/// </para>
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public abstract class ApiController : ControllerBase;
