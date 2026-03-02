using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Web;
using Microsoft.Extensions.Logging;
using MyProject.Infrastructure.Features.Authentication.Options;

namespace MyProject.Infrastructure.Features.Authentication.Services.ExternalProviders;

/// <summary>
/// Google OIDC authentication provider.
/// Exchanges authorization codes via the token endpoint, then calls the <c>/userinfo</c> endpoint
/// with the access token to retrieve verified user identity (sub, email, name).
/// </summary>
internal sealed class GoogleAuthProvider(
    IHttpClientFactory httpClientFactory,
    ExternalAuthOptions.ProviderOptions options,
    ILogger<GoogleAuthProvider> logger) : IExternalAuthProvider
{
    private const string AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string TokenEndpoint = "https://oauth2.googleapis.com/token";
    private const string UserInfoEndpoint = "https://www.googleapis.com/oauth2/v3/userinfo";
    private const string HttpClientName = "Google-OAuth";

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
        using var httpClient = httpClientFactory.CreateClient(HttpClientName);

        using var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
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

        if (string.IsNullOrEmpty(tokenResult.AccessToken))
        {
            throw new InvalidOperationException("Google token response did not contain an access_token.");
        }

        return await GetUserInfoAsync(httpClient, tokenResult.AccessToken, cancellationToken);
    }

    /// <summary>
    /// Calls Google's userinfo endpoint with the access token to retrieve verified identity claims.
    /// This avoids parsing the id_token JWT directly and delegates verification to Google's servers.
    /// </summary>
    private async Task<ExternalUserInfo> GetUserInfoAsync(
        HttpClient httpClient, string accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, UserInfoEndpoint);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("Google userinfo request failed with {StatusCode}: {Body}",
                response.StatusCode, errorBody);
            throw new InvalidOperationException("Google userinfo request failed.");
        }

        var userInfo = await response.Content.ReadFromJsonAsync<GoogleUserInfo>(cancellationToken)
            ?? throw new InvalidOperationException("Google returned an empty userinfo response.");

        return new ExternalUserInfo(
            userInfo.Sub ?? throw new InvalidOperationException("Google userinfo missing 'sub' claim."),
            userInfo.Email ?? throw new InvalidOperationException("Google userinfo missing 'email' claim."),
            userInfo.EmailVerified,
            userInfo.GivenName,
            userInfo.FamilyName);
    }

    private sealed class GoogleTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; init; }
    }

    private sealed class GoogleUserInfo
    {
        [JsonPropertyName("sub")]
        public string? Sub { get; init; }

        [JsonPropertyName("email")]
        public string? Email { get; init; }

        [JsonPropertyName("email_verified")]
        public bool EmailVerified { get; init; }

        [JsonPropertyName("given_name")]
        public string? GivenName { get; init; }

        [JsonPropertyName("family_name")]
        public string? FamilyName { get; init; }
    }
}
