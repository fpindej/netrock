using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Web;
using Microsoft.Extensions.Logging;
using MyProject.Infrastructure.Features.Authentication.Options;

namespace MyProject.Infrastructure.Features.Authentication.Services.ExternalProviders;

/// <summary>
/// Google OIDC authentication provider.
/// Exchanges authorization codes via the token endpoint and decodes the <c>id_token</c> JWT
/// to extract user identity (sub, email, name).
/// </summary>
internal sealed class GoogleAuthProvider(
    HttpClient httpClient,
    ExternalAuthOptions.ProviderOptions options,
    ILogger<GoogleAuthProvider> logger) : IExternalAuthProvider
{
    private const string AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string TokenEndpoint = "https://oauth2.googleapis.com/token";

    /// <inheritdoc />
    public string Name => "Google";

    /// <inheritdoc />
    public string DisplayName => "Google";

    /// <inheritdoc />
    public string BuildAuthorizationUrl(string state, string redirectUri, string? nonce = null)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        query["client_id"] = options.ClientId;
        query["redirect_uri"] = redirectUri;
        query["response_type"] = "code";
        query["scope"] = "openid email profile";
        query["state"] = state;
        query["access_type"] = "online";

        if (nonce is not null)
        {
            query["nonce"] = nonce;
        }

        return $"{AuthorizationEndpoint}?{query}";
    }

    /// <inheritdoc />
    public async Task<ExternalUserInfo> ExchangeCodeAsync(
        string code, string redirectUri, CancellationToken cancellationToken)
    {
        var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = options.ClientId,
            ["client_secret"] = options.ClientSecret,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code"
        });

        var tokenResponse = await httpClient.PostAsync(TokenEndpoint, tokenRequest, cancellationToken);

        if (!tokenResponse.IsSuccessStatusCode)
        {
            var errorBody = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("Google token exchange failed with {StatusCode}: {Body}",
                tokenResponse.StatusCode, errorBody);
            throw new InvalidOperationException("Google token exchange failed.");
        }

        var tokenResult = await tokenResponse.Content.ReadFromJsonAsync<GoogleTokenResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Google returned an empty token response.");

        if (string.IsNullOrEmpty(tokenResult.IdToken))
        {
            throw new InvalidOperationException("Google token response did not contain an id_token.");
        }

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(tokenResult.IdToken);

        var sub = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value
            ?? throw new InvalidOperationException("Google id_token missing 'sub' claim.");
        var email = jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value
            ?? throw new InvalidOperationException("Google id_token missing 'email' claim.");
        var emailVerified = jwt.Claims.FirstOrDefault(c => c.Type == "email_verified")?.Value == "true";
        var givenName = jwt.Claims.FirstOrDefault(c => c.Type == "given_name")?.Value;
        var familyName = jwt.Claims.FirstOrDefault(c => c.Type == "family_name")?.Value;

        return new ExternalUserInfo(sub, email, emailVerified, givenName, familyName);
    }

    private sealed class GoogleTokenResponse
    {
        [JsonPropertyName("id_token")]
        public string? IdToken { get; init; }
    }
}
