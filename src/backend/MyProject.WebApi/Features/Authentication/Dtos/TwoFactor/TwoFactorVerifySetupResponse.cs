using JetBrains.Annotations;

namespace MyProject.WebApi.Features.Authentication.Dtos.TwoFactor;

/// <summary>
/// Response containing recovery codes after enabling 2FA or regenerating codes.
/// </summary>
public class TwoFactorVerifySetupResponse
{
    /// <summary>
    /// The one-time recovery codes. Each code can only be used once. Store them securely.
    /// </summary>
    public IReadOnlyList<string> RecoveryCodes { [UsedImplicitly] get; [UsedImplicitly] init; } = [];
}
