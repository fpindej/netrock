namespace MyProject.Application.Features.Authentication.Dtos;

/// <summary>
/// Output from the login operation. Either contains authentication tokens (no 2FA or 2FA completed)
/// or a challenge token indicating that two-factor authentication is required.
/// </summary>
/// <param name="Tokens">The authentication tokens, or <c>null</c> when 2FA is required.</param>
/// <param name="ChallengeToken">The opaque challenge token for completing 2FA, or <c>null</c> when 2FA is not required.</param>
/// <param name="RequiresTwoFactor">Whether the user must complete a two-factor authentication step.</param>
public record LoginOutput(
    AuthenticationOutput? Tokens,
    string? ChallengeToken,
    bool RequiresTwoFactor
);
