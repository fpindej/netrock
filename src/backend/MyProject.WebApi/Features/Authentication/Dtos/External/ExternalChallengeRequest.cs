using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace MyProject.WebApi.Features.Authentication.Dtos.External;

/// <summary>
/// Request to initiate an external OAuth2 authorization flow.
/// </summary>
public class ExternalChallengeRequest
{
    /// <summary>
    /// The provider name (e.g. "Google", "GitHub").
    /// </summary>
    [Required]
    [MaxLength(32)]
    public string Provider { get; [UsedImplicitly] init; } = string.Empty;

    /// <summary>
    /// The client's callback URI where the provider will redirect after authorization.
    /// Must be in the server's AllowedRedirectUris whitelist.
    /// </summary>
    [Required]
    [MaxLength(2048)]
    public string RedirectUri { get; [UsedImplicitly] init; } = string.Empty;
}
