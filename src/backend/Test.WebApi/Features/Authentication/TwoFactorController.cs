using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Test.Application.Features.Authentication;
using Test.WebApi.Features.Authentication.Dtos.Login;
using Test.WebApi.Features.Authentication.Dtos.TwoFactor;
using Test.WebApi.Shared;

namespace Test.WebApi.Features.Authentication;

/// <summary>
/// Two-factor authentication operations: 2FA login, setup, disable, and recovery code management.
/// </summary>
[ApiController]
[Route("api/auth/two-factor")]
[Tags("Auth - Two-Factor")]
public class TwoFactorController(
    IAuthenticationService authenticationService,
    ITwoFactorService twoFactorService) : ControllerBase
{
    /// <summary>
    /// Completes a two-factor login using a TOTP code from an authenticator app.
    /// </summary>
    [HttpPost("login")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AuthenticationResponse>> Login(
        [FromBody] TwoFactorLoginRequest request,
        [FromQuery] bool useCookies = false,
        CancellationToken cancellationToken = default)
    {
        var result = await authenticationService.CompleteTwoFactorLoginAsync(
            request.ChallengeToken, request.Code, useCookies, cancellationToken);

        if (!result.IsSuccess)
        {
            return ProblemFactory.Create(result.Error, result.ErrorType);
        }

        return Ok(result.Value.ToResponse());
    }

    /// <summary>
    /// Completes a two-factor login using a one-time recovery code.
    /// </summary>
    [HttpPost("login/recovery")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AuthenticationResponse>> RecoveryLogin(
        [FromBody] TwoFactorRecoveryLoginRequest request,
        [FromQuery] bool useCookies = false,
        CancellationToken cancellationToken = default)
    {
        var result = await authenticationService.CompleteTwoFactorRecoveryLoginAsync(
            request.ChallengeToken, request.RecoveryCode, useCookies, cancellationToken);

        if (!result.IsSuccess)
        {
            return ProblemFactory.Create(result.Error, result.ErrorType);
        }

        return Ok(result.Value.ToResponse());
    }

    /// <summary>
    /// Generates an authenticator key and URI for setting up 2FA.
    /// </summary>
    [Authorize]
    [HttpPost("setup")]
    [EnableRateLimiting(RateLimitPolicies.Sensitive)]
    [ProducesResponseType(typeof(TwoFactorSetupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<TwoFactorSetupResponse>> Setup(CancellationToken cancellationToken)
    {
        var result = await twoFactorService.SetupAsync(cancellationToken);

        if (!result.IsSuccess)
        {
            return ProblemFactory.Create(result.Error, result.ErrorType);
        }

        return Ok(result.Value.ToResponse());
    }

    /// <summary>
    /// Verifies a TOTP code to complete 2FA setup and returns recovery codes.
    /// </summary>
    [Authorize]
    [HttpPost("verify-setup")]
    [EnableRateLimiting(RateLimitPolicies.Sensitive)]
    [ProducesResponseType(typeof(TwoFactorVerifySetupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<TwoFactorVerifySetupResponse>> VerifySetup(
        [FromBody] TwoFactorVerifySetupRequest request,
        CancellationToken cancellationToken)
    {
        var result = await twoFactorService.VerifySetupAsync(request.Code, cancellationToken);

        if (!result.IsSuccess)
        {
            return ProblemFactory.Create(result.Error, result.ErrorType);
        }

        return Ok(result.Value.ToResponse());
    }

    /// <summary>
    /// Disables two-factor authentication for the current user. Requires password confirmation.
    /// </summary>
    [Authorize]
    [HttpPost("disable")]
    [EnableRateLimiting(RateLimitPolicies.Sensitive)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult> Disable(
        [FromBody] TwoFactorDisableRequest request,
        CancellationToken cancellationToken)
    {
        var result = await twoFactorService.DisableAsync(request.Password, cancellationToken);

        if (!result.IsSuccess)
        {
            return ProblemFactory.Create(result.Error, result.ErrorType);
        }

        return NoContent();
    }

    /// <summary>
    /// Regenerates two-factor recovery codes. Requires password confirmation.
    /// </summary>
    [Authorize]
    [HttpPost("recovery-codes")]
    [EnableRateLimiting(RateLimitPolicies.Sensitive)]
    [ProducesResponseType(typeof(TwoFactorVerifySetupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<TwoFactorVerifySetupResponse>> RegenerateCodes(
        [FromBody] TwoFactorRegenerateCodesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await twoFactorService.RegenerateRecoveryCodesAsync(request.Password, cancellationToken);

        if (!result.IsSuccess)
        {
            return ProblemFactory.Create(result.Error, result.ErrorType);
        }

        return Ok(result.Value.ToResponse());
    }
}
