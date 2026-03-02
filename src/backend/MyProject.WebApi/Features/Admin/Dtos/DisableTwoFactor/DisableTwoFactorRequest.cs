using JetBrains.Annotations;

namespace MyProject.WebApi.Features.Admin.Dtos.DisableTwoFactor;

/// <summary>
/// Request to disable two-factor authentication for a user.
/// </summary>
public class DisableTwoFactorRequest
{
    /// <summary>
    /// Optional reason for disabling two-factor authentication.
    /// </summary>
    public string? Reason { get; [UsedImplicitly] init; }
}
