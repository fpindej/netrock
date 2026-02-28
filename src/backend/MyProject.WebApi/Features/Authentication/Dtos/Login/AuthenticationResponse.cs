using JetBrains.Annotations;

namespace MyProject.WebApi.Features.Authentication.Dtos.Login;

/// <summary>
/// Response containing authentication tokens for API clients.
/// Web clients can ignore this response body as tokens are also set in HttpOnly cookies.
/// </summary>
public class AuthenticationResponse
{
    /// <summary>
    /// The JWT access token for Bearer authentication.
    /// Include this in the Authorization header as "Bearer {accessToken}" for subsequent API requests.
    /// Empty when <see cref="RequiresTwoFactor"/> is true.
    /// </summary>
    public string AccessToken { [UsedImplicitly] get; [UsedImplicitly] init; } = string.Empty;

    /// <summary>
    /// The refresh token for obtaining new access tokens.
    /// Use this with the /api/auth/refresh endpoint when the access token expires.
    /// Empty when <see cref="RequiresTwoFactor"/> is true.
    /// </summary>
    public string RefreshToken { [UsedImplicitly] get; [UsedImplicitly] init; } = string.Empty;

    /// <summary>
    /// Whether two-factor authentication is required to complete the login.
    /// When true, use the <see cref="ChallengeToken"/> with POST /api/auth/login/2fa.
    /// </summary>
    public bool RequiresTwoFactor { [UsedImplicitly] get; [UsedImplicitly] init; }

    /// <summary>
    /// The opaque challenge token for completing the 2FA login step.
    /// Null when <see cref="RequiresTwoFactor"/> is false.
    /// </summary>
    public string? ChallengeToken { [UsedImplicitly] get; [UsedImplicitly] init; }
}
