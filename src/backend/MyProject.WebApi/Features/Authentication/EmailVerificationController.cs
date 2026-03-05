using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MyProject.Application.Features.Authentication;
using MyProject.WebApi.Features.Authentication.Dtos.VerifyEmail;
using MyProject.WebApi.Shared;

namespace MyProject.WebApi.Features.Authentication;

/// <summary>
/// Email verification operations: verify email and resend verification.
/// </summary>
[ApiController]
[Route("api/auth/email")]
[Tags("Auth - Email Verification")]
public class EmailVerificationController(
    IAuthenticationService authenticationService) : ControllerBase
{
    /// <summary>
    /// Verifies a user's email address using a confirmation token received via email.
    /// </summary>
    [HttpPost("verify")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult> Verify([FromBody] VerifyEmailRequest request, CancellationToken cancellationToken)
    {
        var result = await authenticationService.VerifyEmailAsync(request.ToVerifyEmailInput(), cancellationToken);

        if (!result.IsSuccess)
        {
            return ProblemFactory.Create(result.Error, result.ErrorType);
        }

        return Ok();
    }

    /// <summary>
    /// Resends a verification email to the current authenticated user.
    /// Fails if the user's email is already verified.
    /// </summary>
    [Authorize]
    [HttpPost("resend-verification")]
    [EnableRateLimiting(RateLimitPolicies.Sensitive)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult> ResendVerification(CancellationToken cancellationToken)
    {
        var result = await authenticationService.ResendVerificationEmailAsync(cancellationToken);

        if (!result.IsSuccess)
        {
            return ProblemFactory.Create(result.Error, result.ErrorType);
        }

        return Ok();
    }
}
