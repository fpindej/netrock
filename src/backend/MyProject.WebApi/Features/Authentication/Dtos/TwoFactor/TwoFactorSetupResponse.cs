using JetBrains.Annotations;

namespace MyProject.WebApi.Features.Authentication.Dtos.TwoFactor;

/// <summary>
/// Response containing the shared key and authenticator URI for 2FA setup.
/// </summary>
public class TwoFactorSetupResponse
{
    /// <summary>
    /// The base32-encoded shared secret for manual entry into an authenticator app.
    /// </summary>
    public string SharedKey { [UsedImplicitly] get; [UsedImplicitly] init; } = string.Empty;

    /// <summary>
    /// The otpauth:// URI for QR code scanning by authenticator apps.
    /// </summary>
    public string AuthenticatorUri { [UsedImplicitly] get; [UsedImplicitly] init; } = string.Empty;
}
