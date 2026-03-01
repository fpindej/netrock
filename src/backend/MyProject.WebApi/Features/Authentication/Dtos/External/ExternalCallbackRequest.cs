using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace MyProject.WebApi.Features.Authentication.Dtos.External;

/// <summary>
/// Request to handle the OAuth2 provider callback with authorization code and state.
/// </summary>
public class ExternalCallbackRequest
{
    /// <summary>
    /// The authorization code received from the OAuth2 provider.
    /// </summary>
    [Required]
    [MaxLength(2048)]
    public string Code { get; [UsedImplicitly] init; } = string.Empty;

    /// <summary>
    /// The state token for CSRF validation, matching the one sent in the challenge.
    /// </summary>
    [Required]
    [MaxLength(512)]
    public string State { get; [UsedImplicitly] init; } = string.Empty;
}
