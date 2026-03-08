using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Web;
using Microsoft.Extensions.Logging;
using Test.Shared;

namespace Test.Infrastructure.Features.Authentication.Services.ExternalProviders;

/// <summary>
/// Twitch OIDC authentication provider.
/// Exchanges authorization codes via the token endpoint, then calls the <c>/oauth2/userinfo</c> endpoint
/// with both a Bearer token and <c>Client-Id</c> header to retrieve user identity.
/// Twitch does not provide first/last name - <c>preferred_username</c> is used as the first name.
/// </summary>
internal sealed class TwitchAuthProvider(
    IHttpClientFactory httpClientFactory,
    ILogger<TwitchAuthProvider> logger) : IExternalAuthProvider
{
    private const string AuthorizationEndpoint = "https://id.twitch.tv/oauth2/authorize";
    private const string TokenEndpoint = "https://id.twitch.tv/oauth2/token";
    private const string UserInfoEndpoint = "https://id.twitch.tv/oauth2/userinfo";
    internal const string HttpClientName = "Twitch-OAuth";

    /// <inheritdoc />
    public string Name => "Twitch";

    /// <inheritdoc />
    public string DisplayName => "Twitch";

    /// <inheritdoc />
    public string BuildAuthorizationUrl(ProviderCredentials credentials, string state, string redirectUri, string? nonce = null)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        query["client_id"] = credentials.ClientId;
        query["redirect_uri"] = redirectUri;
        query["response_type"] = "code";
        query["scope"] = "openid user:read:email";
        query["state"] = state;

        if (nonce is not null)
        {
            query["nonce"] = nonce;
        }

        return $"{AuthorizationEndpoint}?{query}";
    }

    /// <inheritdoc />
    public async Task<ExternalUserInfo> ExchangeCodeAsync(
        ProviderCredentials credentials, string code, string redirectUri, CancellationToken cancellationToken)
    {
        using var httpClient = httpClientFactory.CreateClient(HttpClientName);

        using var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = credentials.ClientId,
            ["client_secret"] = credentials.ClientSecret,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code"
        });

        using var tokenResponse = await httpClient.PostAsync(TokenEndpoint, tokenRequest, cancellationToken);

        if (!tokenResponse.IsSuccessStatusCode)
        {
            var errorBody = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning("Twitch token exchange failed with {StatusCode}: {Body}",
                tokenResponse.StatusCode, Truncate(errorBody));
            throw new InvalidOperationException("Twitch token exchange failed.");
        }

        var tokenResult = await tokenResponse.Content.ReadFromJsonAsync<TwitchTokenResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Twitch returned an empty token response.");

        if (string.IsNullOrEmpty(tokenResult.AccessToken))
        {
            throw new InvalidOperationException("Twitch token response did not contain an access_token.");
        }

        return await GetUserInfoAsync(httpClient, credentials.ClientId, tokenResult.AccessToken, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result> TestConnectionAsync(ProviderCredentials credentials, CancellationToken cancellationToken)
    {
        try
        {
            using var httpClient = httpClientFactory.CreateClient(HttpClientName);
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            using var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["code"] = "__test_connection__",
                ["client_id"] = credentials.ClientId,
                ["client_secret"] = credentials.ClientSecret,
                ["redirect_uri"] = "https://localhost/test",
                ["grant_type"] = "authorization_code"
            });

            using var response = await httpClient.PostAsync(TokenEndpoint, tokenRequest, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return Result.Failure(ErrorMessages.ExternalAuth.TestConnectionInvalidCredentials);
            }

            var body = await response.Content.ReadFromJsonAsync<TokenErrorResponse>(cancellationToken);
            if (body?.Error is "invalid_client")
            {
                return Result.Failure(ErrorMessages.ExternalAuth.TestConnectionInvalidCredentials);
            }

            return Result.Success();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Twitch test connection failed");
            return Result.Failure(ErrorMessages.ExternalAuth.TestConnectionProviderUnreachable);
        }
    }

    /// <summary>
    /// Calls Twitch's OIDC userinfo endpoint with both a Bearer token and <c>Client-Id</c> header.
    /// Twitch requires the <c>Client-Id</c> header on all API requests.
    /// </summary>
    private async Task<ExternalUserInfo> GetUserInfoAsync(
        HttpClient httpClient, string clientId, string accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, UserInfoEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("Client-Id", clientId);

        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning("Twitch userinfo request failed with {StatusCode}: {Body}",
                response.StatusCode, Truncate(errorBody));
            throw new InvalidOperationException("Twitch userinfo request failed.");
        }

        var userInfo = await response.Content.ReadFromJsonAsync<TwitchUserInfo>(cancellationToken)
            ?? throw new InvalidOperationException("Twitch returned an empty userinfo response.");

        return new ExternalUserInfo(
            userInfo.Sub ?? throw new InvalidOperationException("Twitch userinfo missing 'sub' claim."),
            userInfo.Email ?? throw new InvalidOperationException("Twitch userinfo missing 'email' claim."),
            userInfo.EmailVerified,
            userInfo.PreferredUsername,
            null);
    }

    private sealed class TwitchTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; init; }
    }

    private sealed class TokenErrorResponse
    {
        [JsonPropertyName("error")]
        public string? Error { get; init; }
    }

    private sealed class TwitchUserInfo
    {
        [JsonPropertyName("sub")]
        public string? Sub { get; init; }

        [JsonPropertyName("email")]
        public string? Email { get; init; }

        [JsonPropertyName("email_verified")]
        public bool EmailVerified { get; init; }

        [JsonPropertyName("preferred_username")]
        public string? PreferredUsername { get; init; }
    }

    /// <summary>
    /// Truncates provider error response bodies to prevent PII leakage in logs.
    /// </summary>
    private static string Truncate(string value, int maxLength = 200) =>
        value.Length <= maxLength ? value : value[..maxLength] + "...[truncated]";
}
