namespace MyProject.Application.Features.Authentication.Dtos;

/// <summary>
/// Input for handling the OAuth2 callback after provider authorization.
/// </summary>
/// <param name="Code">The authorization code from the provider.</param>
/// <param name="State">The state token for CSRF validation.</param>
public record ExternalCallbackInput(string Code, string State);
