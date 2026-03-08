using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Test.Application.Features.Authentication;
using Test.Application.Features.Captcha;
using Test.Shared;
using Test.WebApi.Features.Authentication.Dtos.ChangePassword;
using Test.WebApi.Features.Authentication.Dtos.ForgotPassword;
using Test.WebApi.Features.Authentication.Dtos.ResetPassword;
using Test.WebApi.Shared;

namespace Test.WebApi.Features.Authentication;

/// <summary>
/// Password management operations: forgot password, reset password, and change password.
/// </summary>
[ApiController]
[Route("api/auth/password")]
[Tags("Auth - Password")]
public class PasswordController(
    IAuthenticationService authenticationService,
    ICaptchaService captchaService) : ControllerBase
{
    /// <summary>
    /// Initiates a password reset flow by sending a reset link to the provided email address.
    /// Always returns 200 regardless of whether the email exists to prevent user enumeration.
    /// </summary>
    [HttpPost("forgot")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult> Forgot([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        if (!await captchaService.ValidateTokenAsync(request.CaptchaToken, cancellationToken))
        {
            return ProblemFactory.Create(ErrorMessages.Auth.CaptchaInvalid, ErrorType.Validation);
        }

        await authenticationService.ForgotPasswordAsync(request.Email, cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Resets a user's password using a token received via email.
    /// Revokes all existing refresh tokens to force re-authentication on other devices.
    /// </summary>
    [HttpPost("reset")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult> Reset([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await authenticationService.ResetPasswordAsync(request.ToResetPasswordInput(), cancellationToken);

        if (!result.IsSuccess)
        {
            return ProblemFactory.Create(result.Error, result.ErrorType);
        }

        return Ok();
    }

    /// <summary>
    /// Changes the current authenticated user's password.
    /// Revokes all existing refresh tokens to force re-authentication on other devices.
    /// </summary>
    [Authorize]
    [HttpPost("change")]
    [EnableRateLimiting(RateLimitPolicies.Sensitive)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult> Change([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await authenticationService.ChangePasswordAsync(request.ToChangePasswordInput(), cancellationToken);

        if (!result.IsSuccess)
        {
            return ProblemFactory.Create(result.Error, result.ErrorType);
        }

        return NoContent();
    }
}
