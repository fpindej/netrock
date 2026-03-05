using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MyProject.Application.Cookies.Constants;
using MyProject.Application.Features.Authentication;
using MyProject.Application.Features.Captcha;
using MyProject.Shared;
using MyProject.WebApi.Features.Authentication.Dtos.Login;
using MyProject.WebApi.Features.Authentication.Dtos.Register;
using MyProject.WebApi.Shared;

namespace MyProject.WebApi.Features.Authentication;

/// <summary>
/// Core authentication operations: login, registration, token refresh, and logout.
/// Supports both cookie-based (web) and Bearer token (mobile/API) authentication.
/// </summary>
[ApiController]
[Route("api/auth")]
[Tags("Auth")]
public class AuthController(
    IAuthenticationService authenticationService,
    ICaptchaService captchaService) : ControllerBase
{
    /// <summary>
    /// Authenticates a user and returns JWT tokens, or a 2FA challenge if two-factor is enabled.
    /// Tokens are always returned in the response body. When useCookies is true, tokens are also set as HttpOnly cookies.
    /// </summary>
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
    /// Registers a new user account.
    /// </summary>
    [HttpPost("register")]
    [EnableRateLimiting(RateLimitPolicies.Registration)]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        if (!await captchaService.ValidateTokenAsync(request.CaptchaToken, cancellationToken))
        {
            return ProblemFactory.Create(ErrorMessages.Auth.CaptchaInvalid, ErrorType.Validation);
        }

        var result = await authenticationService.Register(request.ToRegisterInput(), cancellationToken);

        if (!result.IsSuccess)
        {
            return ProblemFactory.Create(result.Error, result.ErrorType);
        }

        var response = new RegisterResponse { Id = result.Value };
        return Created(string.Empty, response);
    }
}
