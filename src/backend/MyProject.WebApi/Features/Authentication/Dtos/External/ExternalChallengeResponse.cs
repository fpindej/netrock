using JetBrains.Annotations;

namespace MyProject.WebApi.Features.Authentication.Dtos.External;

/// <summary>
/// Response containing the OAuth2 authorization URL to redirect the user to.
/// </summary>
public class ExternalChallengeResponse
{
    /// <summary>
    /// The provider's authorization URL that the client should navigate to.
    /// </summary>
    public string AuthorizationUrl { [UsedImplicitly] get; [UsedImplicitly] init; } = string.Empty;
}
