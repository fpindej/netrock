using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MyProject.Application.Cookies.Constants;
using MyProject.Application.Features.Authentication;
using MyProject.Shared;
using MyProject.WebApi.Features.Authentication.Dtos.ChangePassword;
using MyProject.WebApi.Features.Authentication.Dtos.ForgotPassword;
using MyProject.WebApi.Features.Authentication.Dtos.Login;
using MyProject.WebApi.Features.Authentication.Dtos.Register;
using MyProject.WebApi.Features.Authentication.Dtos.ResetPassword;
using MyProject.WebApi.Features.Authentication.Dtos.VerifyEmail;
using MyProject.WebApi.Shared;

namespace MyProject.WebApi.Features.Authentication;

/// <summary>
/// Controller for authentication operations including login, registration, and token management.
/// Supports both cookie-based (web) and Bearer token (mobile/API) authentication.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Tags("Auth")]
public class AuthController(IAuthenticationService authenticationService) : ControllerBase
{
    /// <summary>
    /// Authenticates a user and returns JWT tokens.
    /// Tokens are always returned in the response body. When useCookies is true, tokens are also set as HttpOnly cookies.
    /// </summary>
    /// <param name="request">The login credentials</param>
    /// <param name="useCookies">When true, sets tokens in HttpOnly cookies for web clients. Defaults to false (stateless).</param>
    /// <returns>Authentication response containing access and refresh tokens</returns>
    /// <response code="200">Returns authentication tokens (optionally also set in HttpOnly cookies)</response>
    /// <response code="400">If the credentials are improperly formatted</response>
    /// <response code="401">If the credentials are invalid</response>
    [HttpPost("login")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AuthenticationResponse>> Login(
        [FromBody] LoginRequest request,
        [FromQuery] bool useCookies = false,
        CancellationToken cancellationToken = default)
    {
        var result = await authenticationService.Login(request.Username, request.Password, useCookies, request.RememberMe, cancellationToken);

        if (!result.IsSuccess)
        {
            return ProblemFactory.Create(result.Error, result.ErrorType);
        }

        return Ok(result.Value.ToResponse());
    }

    /// <summary>
    /// Refreshes the authentication tokens using a refresh token.
    /// For web clients, the refresh token is read from cookies. For mobile/API clients, pass it in the request body.
    /// When useCookies is true, new tokens are also set as HttpOnly cookies.
    /// </summary>
    /// <param name="request">Optional request body containing the refresh token (for mobile/API clients)</param>
    /// <param name="useCookies">When true, sets tokens in HttpOnly cookies for web clients. Defaults to false (stateless).</param>
    /// <returns>Authentication response containing new access and refresh tokens</returns>
    /// <response code="200">Returns new authentication tokens (optionally also set in HttpOnly cookies)</response>
    /// <response code="400">If the request body is malformed</response>
    /// <response code="401">If the refresh token is invalid, expired, or missing</response>
    [HttpPost("refresh")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AuthenticationResponse>> Refresh(
        [FromBody] RefreshRequest? request,
        [FromQuery] bool useCookies = false,
        CancellationToken cancellationToken = default)
    {
        // Priority 1: Request body (mobile/API clients)
        // Priority 2: Cookie (web clients)
        var refreshToken = request?.RefreshToken;
        if (string.IsNullOrEmpty(refreshToken))
        {
            Request.Cookies.TryGetValue(CookieNames.RefreshToken, out refreshToken);
        }

        if (string.IsNullOrEmpty(refreshToken))
        {
            return ProblemFactory.Create(ErrorMessages.Auth.TokenMissing, ErrorType.Unauthorized);
        }

        var result = await authenticationService.RefreshTokenAsync(refreshToken, useCookies, cancellationToken);

        if (!result.IsSuccess)
        {
            return ProblemFactory.Create(result.Error, result.ErrorType);
        }

        return Ok(result.Value.ToResponse());
    }

    /// <summary>
    /// Logs out the current user by revoking refresh tokens and clearing authentication cookies.
    /// </summary>
    /// <returns>A 204 No Content response</returns>
    /// <response code="204">Successfully logged out</response>
    /// <response code="401">If the user is not authenticated</response>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> Logout(CancellationToken cancellationToken)
    {
        await authenticationService.Logout(cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Registers a new user account
    /// </summary>
    /// <param name="request">The registration details</param>
    /// <returns>Created response with the new user's ID</returns>
    /// <response code="201">User successfully created</response>
    /// <response code="400">If the registration data is invalid</response>
    /// <response code="429">If too many registration requests have been made</response>
    [HttpPost("register")]
    [EnableRateLimiting(RateLimitPolicies.Registration)]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await authenticationService.Register(request.ToRegisterInput(), cancellationToken);

        if (!result.IsSuccess)
        {
            return ProblemFactory.Create(result.Error, result.ErrorType);
        }

        var response = new RegisterResponse { Id = result.Value };
        return Created(string.Empty, response);
    }

    /// <summary>
    /// Initiates a password reset flow by sending a reset link to the provided email address.
    /// Always returns 200 regardless of whether the email exists to prevent user enumeration.
    /// </summary>
    /// <param name="request">The forgot password request containing the email address</param>
    /// <response code="200">Request accepted (email sent if account exists)</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="429">If too many requests have been made</response>
    [HttpPost("forgot-password")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await authenticationService.ForgotPasswordAsync(request.Email, cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Resets a user's password using a token received via email.
    /// Revokes all existing refresh tokens to force re-authentication on other devices.
    /// </summary>
    /// <param name="request">The reset password request containing email, token, and new password</param>
    /// <response code="200">Password reset successfully</response>
    /// <response code="400">If the request is invalid or the token is expired/invalid</response>
    /// <response code="429">If too many requests have been made</response>
    [HttpPost("reset-password")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await authenticationService.ResetPasswordAsync(request.ToResetPasswordInput(), cancellationToken);

        if (!result.IsSuccess)
        {
            return ProblemFactory.Create(result.Error, result.ErrorType);
        }

        return Ok();
    }

    /// <summary>
    /// Verifies a user's email address using a confirmation token received via email.
    /// </summary>
    /// <param name="request">The email verification request containing email and token</param>
    /// <response code="200">Email verified successfully</response>
    /// <response code="400">If the request is invalid or the token is expired/invalid</response>
    /// <response code="429">If too many requests have been made</response>
    [HttpPost("verify-email")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult> VerifyEmail([FromBody] VerifyEmailRequest request, CancellationToken cancellationToken)
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
    /// <response code="200">Verification email sent</response>
    /// <response code="400">If the email is already verified</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="429">If too many requests have been made</response>
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

    /// <summary>
    /// Changes the current authenticated user's password.
    /// Revokes all existing refresh tokens to force re-authentication on other devices.
    /// </summary>
    /// <param name="request">The change password request containing current and new passwords</param>
    /// <returns>A 204 No Content response on success</returns>
    /// <response code="204">Password changed successfully</response>
    /// <response code="400">If the request is invalid or the current password is incorrect</response>
    /// <response code="401">If the user is not authenticated</response>
    [Authorize]
    [HttpPost("change-password")]
    [EnableRateLimiting(RateLimitPolicies.Sensitive)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await authenticationService.ChangePasswordAsync(request.ToChangePasswordInput(), cancellationToken);

        if (!result.IsSuccess)
        {
            return ProblemFactory.Create(result.Error, result.ErrorType);
        }

        return NoContent();
    }
}
