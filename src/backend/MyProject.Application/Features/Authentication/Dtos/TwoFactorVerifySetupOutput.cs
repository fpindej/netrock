namespace MyProject.Application.Features.Authentication.Dtos;

/// <summary>
/// Output from 2FA setup verification or recovery code regeneration,
/// containing the one-time-use recovery codes.
/// </summary>
/// <param name="RecoveryCodes">The recovery codes for backup access. Each code can only be used once.</param>
public record TwoFactorVerifySetupOutput(
    IReadOnlyList<string> RecoveryCodes
);
