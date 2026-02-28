using MyProject.Application.Features.Authentication.Dtos;
using MyProject.Shared;

namespace MyProject.Application.Features.Authentication;

/// <summary>
/// Provides two-factor authentication management operations including setup, verification, and recovery codes.
/// </summary>
public interface ITwoFactorService
{
    /// <summary>
    /// Generates an authenticator key and otpauth:// URI for the current user to set up 2FA.
    /// </summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A result containing the shared key and authenticator URI.</returns>
    Task<Result<TwoFactorSetupOutput>> SetupAsync(CancellationToken ct);

    /// <summary>
    /// Verifies a TOTP code and enables 2FA for the current user, returning recovery codes.
    /// </summary>
    /// <param name="code">The 6-digit TOTP code from the authenticator app.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A result containing the one-time recovery codes on success.</returns>
    Task<Result<TwoFactorVerifySetupOutput>> VerifySetupAsync(string code, CancellationToken ct);

    /// <summary>
    /// Disables 2FA for the current user after verifying their password.
    /// </summary>
    /// <param name="password">The user's current password for confirmation.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> DisableAsync(string password, CancellationToken ct);

    /// <summary>
    /// Regenerates recovery codes for the current user after verifying their password.
    /// </summary>
    /// <param name="password">The user's current password for confirmation.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A result containing the new one-time recovery codes on success.</returns>
    Task<Result<TwoFactorVerifySetupOutput>> RegenerateRecoveryCodesAsync(string password, CancellationToken ct);
}
