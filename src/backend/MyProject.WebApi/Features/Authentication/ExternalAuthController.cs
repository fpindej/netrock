using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MyProject.Application.Features.Authentication;
using MyProject.Shared;
using MyProject.WebApi.Features.Authentication.Dtos.External;
using MyProject.WebApi.Features.Authentication.Dtos.SetPassword;
using MyProject.WebApi.Shared;

namespace MyProject.WebApi.Features.Authentication;

/// <summary>
/// External OAuth2 authentication operations: provider listing, challenge, callback, unlinking, and password setup.
/// </summary>
[ApiController]
[Route("api/auth/external")]
[Tags("Auth - External")]
public class ExternalAuthController(
    IExternalAuthService externalAuthService) : ControllerBase
{
    /// <summary>
    /// Returns the list of enabled external authentication providers.
    /// Clients should call this on startup to determine which OAuth buttons to render.
    /// </summary>
    [HttpGet("providers")]
    [ProducesResponseType(typeof(IReadOnlyList<ExternalProviderResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ExternalProviderResponse>>> GetProviders(
        CancellationToken cancellationToken)
    {
        var providers = await externalAuthService.GetAvailableProvidersAsync(cancellationToken);
        var response = providers.Select(p => p.ToResponse()).ToList();

        return Ok(response);
    }

    /// <summary>
    /// Initiates an external OAuth2 authorization flow by creating a state token
    /// and returning the provider's authorization URL.
    /// </summary>
    [HttpPost("challenge")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType(typeof(ExternalChallengeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ExternalChallengeResponse>> Challenge(
        [FromBody] ExternalChallengeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await externalAuthService.CreateChallengeAsync(request.ToChallengeInput(), cancellationToken);

        if (!result.IsSuccess)
        {
            return ProblemFactory.Create(result.Error, result.ErrorType);
        }

        return Ok(new ExternalChallengeResponse { AuthorizationUrl = result.Value.AuthorizationUrl });
    }

    /// <summary>
    /// Handles the OAuth2 callback after provider authorization.
    /// Validates state, exchanges the code, and performs login/linking/creation.
    /// </summary>
    [HttpPost("callback")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType(typeof(ExternalCallbackResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ExternalCallbackResponse>> Callback(
        [FromBody] ExternalCallbackRequest request,
        [FromQuery] bool useCookies = false,
        CancellationToken cancellationToken = default)
    {
        var result = await externalAuthService.HandleCallbackAsync(
            request.ToCallbackInput(), useCookies, cancellationToken);

        if (!result.IsSuccess)
        {
            return ProblemFactory.Create(result.Error, result.ErrorType);
        }

        return Ok(result.Value.ToResponse());
    }

    /// <summary>
    /// Unlinks an external provider from the current user's account.
    /// Fails if this is the user's only authentication method.
    /// </summary>
    [Authorize]
    [HttpPost("unlink")]
    [EnableRateLimiting(RateLimitPolicies.Sensitive)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult> Unlink(
        [FromBody] ExternalUnlinkRequest request,
        CancellationToken cancellationToken)
    {
        var result = await externalAuthService.UnlinkProviderAsync(request.Provider, cancellationToken);

        if (!result.IsSuccess)
        {
            return ProblemFactory.Create(result.Error, result.ErrorType);
        }

        return NoContent();
    }

    /// <summary>
    /// Sets an initial password for a passwordless OAuth-created account.
    /// Only available when the user has no password set.
    /// </summary>
    [Authorize]
    [HttpPost("set-password")]
    [EnableRateLimiting(RateLimitPolicies.Sensitive)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult> SetPassword(
        [FromBody] SetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await externalAuthService.SetPasswordAsync(request.ToSetPasswordInput(), cancellationToken);

        if (!result.IsSuccess)
        {
            return ProblemFactory.Create(result.Error, result.ErrorType);
        }

        return NoContent();
    }
}
