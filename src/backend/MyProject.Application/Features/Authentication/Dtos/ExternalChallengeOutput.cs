namespace MyProject.Application.Features.Authentication.Dtos;

/// <summary>
/// Output containing the authorization URL the client should navigate to.
/// </summary>
/// <param name="AuthorizationUrl">The provider's authorization URL with all required parameters.</param>
public record ExternalChallengeOutput(string AuthorizationUrl);
