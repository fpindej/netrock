namespace MyProject.Application.Features.Authentication.Dtos;

/// <summary>
/// Output from 2FA setup initiation, containing the shared secret and authenticator URI
/// for QR code generation.
/// </summary>
/// <param name="SharedKey">The base32-encoded shared secret for manual entry.</param>
/// <param name="AuthenticatorUri">The otpauth:// URI for QR code scanning by authenticator apps.</param>
public record TwoFactorSetupOutput(
    string SharedKey,
    string AuthenticatorUri
);
