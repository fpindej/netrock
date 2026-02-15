using MyProject.Application.Features.Authentication.Dtos;
using MyProject.Shared;

namespace MyProject.Application.Features.Authentication;

/// <summary>
/// Provides authentication operations including login, registration, logout, token refresh, and password management.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticates a user with username and password.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="password">The password.</param>
    /// <param name="useCookies">Whether to set authentication cookies. Defaults to false (stateless). Set to true for web clients.</param>
    /// <param name="rememberMe">When true and cookies are enabled, sets persistent cookies that survive browser restarts. Defaults to false (session cookies).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result containing authentication tokens on success.</returns>
    Task<Result<AuthenticationOutput>> Login(string username, string password, bool useCookies = false, bool rememberMe = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a new user.
    /// </summary>
    /// <param name="input">The registration input.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result<Guid>> Register(RegisterInput input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs out the current user by clearing cookies and revoking tokens.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task Logout(CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes the access token using a refresh token.
    /// </summary>
    /// <param name="refreshToken">The refresh token.</param>
    /// <param name="useCookies">Whether to set authentication cookies. Defaults to false (stateless). Set to true for web clients.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result containing new authentication tokens on success.</returns>
    Task<Result<AuthenticationOutput>> RefreshTokenAsync(string refreshToken, bool useCookies = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes the current user's password after verifying the current password.
    /// Revokes all existing refresh tokens to force re-authentication on other devices.
    /// </summary>
    /// <param name="input">The change password input containing current and new passwords.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> ChangePasswordAsync(ChangePasswordInput input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiates a password reset flow by generating a token and sending a reset email.
    /// Always returns success to prevent user enumeration — if the user does not exist, no email is sent.
    /// </summary>
    /// <param name="email">The email address to send the reset link to.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result that is always successful (to prevent user enumeration).</returns>
    Task<Result> ForgotPasswordAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets a user's password using a previously issued reset token.
    /// Revokes all existing refresh tokens to force re-authentication on other devices.
    /// </summary>
    /// <param name="input">The reset password input containing email, token, and new password.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> ResetPasswordAsync(ResetPasswordInput input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies a user's email address using a confirmation token received via email.
    /// </summary>
    /// <param name="input">The verify email input containing email and token.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> VerifyEmailAsync(VerifyEmailInput input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resends a verification email to the current authenticated user.
    /// Fails if the user's email is already verified.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> ResendVerificationEmailAsync(CancellationToken cancellationToken = default);
}
