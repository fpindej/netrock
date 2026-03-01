namespace MyProject.Application.Features.Authentication.Dtos;

/// <summary>
/// Input for initiating an external OAuth2 authorization flow.
/// </summary>
/// <param name="Provider">The provider name (e.g. "Google", "GitHub").</param>
/// <param name="RedirectUri">The client's callback URI for the OAuth redirect.</param>
public record ExternalChallengeInput(string Provider, string RedirectUri);
