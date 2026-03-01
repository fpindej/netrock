using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace MyProject.WebApi.Features.Authentication.Dtos.SetPassword;

/// <summary>
/// Request to set an initial password on a passwordless OAuth-created account.
/// </summary>
public class SetPasswordRequest
{
    /// <summary>
    /// The new password to set.
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string NewPassword { get; [UsedImplicitly] init; } = string.Empty;
}
