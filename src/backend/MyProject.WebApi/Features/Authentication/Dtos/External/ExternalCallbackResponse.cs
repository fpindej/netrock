using JetBrains.Annotations;

namespace MyProject.WebApi.Features.Authentication.Dtos.External;

/// <summary>
/// Response from the external OAuth2 callback handler.
/// </summary>
public class ExternalCallbackResponse
{
    /// <summary>
    /// The JWT access token, or empty if this was a link-only operation.
    /// </summary>
    public string AccessToken { [UsedImplicitly] get; [UsedImplicitly] init; } = string.Empty;

    /// <summary>
    /// The refresh token, or empty if this was a link-only operation.
    /// </summary>
    public string RefreshToken { [UsedImplicitly] get; [UsedImplicitly] init; } = string.Empty;

    /// <summary>
    /// Whether a new user account was created during this flow.
    /// </summary>
    public bool IsNewUser { [UsedImplicitly] get; [UsedImplicitly] init; }

    /// <summary>
    /// The provider name that was used.
    /// </summary>
    public string Provider { [UsedImplicitly] get; [UsedImplicitly] init; } = string.Empty;

    /// <summary>
    /// Whether this was an account-linking operation (user was already logged in).
    /// </summary>
    public bool IsLinkOnly { [UsedImplicitly] get; [UsedImplicitly] init; }
}
