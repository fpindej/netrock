using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Web;
using Microsoft.Extensions.Logging;
using MyProject.Shared;

namespace MyProject.Infrastructure.Features.Authentication.Services.ExternalProviders;

/// <summary>
/// Slack OIDC authentication provider.
/// Exchanges authorization codes via the <c>openid.connect.token</c> API, then calls
/// <c>openid.connect.userInfo</c> to retrieve user identity.
/// Both endpoints wrap responses in an <c>{"ok": true/false, ...}</c> envelope.
/// </summary>
internal sealed class SlackAuthProvider(
    IHttpClientFactory httpClientFactory,
    ILogger<SlackAuthProvider> logger) : IExternalAuthProvider
{
    private const string AuthorizationEndpoint = "https://slack.com/openid/connect/authorize";
    private const string TokenEndpoint = "https://slack.com/api/openid.connect.token";
    private const string UserInfoEndpoint = "https://slack.com/api/openid.connect.userInfo";
    internal const string HttpClientName = "Slack-OAuth";

    /// <inheritdoc />
    public string Name => "Slack";

    /// <inheritdoc />
    public string DisplayName => "Slack";

    /// <inheritdoc />
    public string BuildAuthorizationUrl(ProviderCredentials credentials, string state, string redirectUri, string? nonce = null)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        query["client_id"] = credentials.ClientId;
        query["redirect_uri"] = redirectUri;
        query["response_type"] = "code";
        query["scope"] = "openid email profile";
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
            ["redirect_uri"] = redirectUri
        });

        using var tokenResponse = await httpClient.PostAsync(TokenEndpoint, tokenRequest, cancellationToken);

        if (!tokenResponse.IsSuccessStatusCode)
        {
            var errorBody = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning("Slack token exchange failed with {StatusCode}: {Body}",
                tokenResponse.StatusCode, Truncate(errorBody));
            throw new InvalidOperationException("Slack token exchange failed.");
        }

        var tokenResult = await tokenResponse.Content.ReadFromJsonAsync<SlackTokenResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Slack returned an empty token response.");

        if (!tokenResult.Ok)
        {
            logger.LogWarning("Slack token exchange returned error: {Error}", tokenResult.Error);
            throw new InvalidOperationException("Slack token exchange failed.");
        }

        if (string.IsNullOrEmpty(tokenResult.AccessToken))
        {
            throw new InvalidOperationException("Slack token response did not contain an access_token.");
        }

        return await GetUserInfoAsync(httpClient, tokenResult.AccessToken, cancellationToken);
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
                ["redirect_uri"] = "https://localhost/test"
            });

            using var response = await httpClient.PostAsync(TokenEndpoint, tokenRequest, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return Result.Failure(ErrorMessages.ExternalAuth.TestConnectionInvalidCredentials);
            }

            var body = await response.Content.ReadFromJsonAsync<SlackTokenResponse>(cancellationToken);
            if (body is { Ok: false, Error: "invalid_client_id" })
            {
                return Result.Failure(ErrorMessages.ExternalAuth.TestConnectionInvalidCredentials);
            }

            return Result.Success();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Slack test connection failed");
            return Result.Failure(ErrorMessages.ExternalAuth.TestConnectionProviderUnreachable);
        }
    }

    /// <summary>
    /// Calls Slack's OIDC userInfo endpoint to retrieve verified identity claims.
    /// The response is wrapped in Slack's <c>{"ok": true, ...}</c> envelope.
    /// </summary>
    private async Task<ExternalUserInfo> GetUserInfoAsync(
        HttpClient httpClient, string accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, UserInfoEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning("Slack userinfo request failed with {StatusCode}: {Body}",
                response.StatusCode, Truncate(errorBody));
            throw new InvalidOperationException("Slack userinfo request failed.");
        }

        var userInfo = await response.Content.ReadFromJsonAsync<SlackUserInfo>(cancellationToken)
            ?? throw new InvalidOperationException("Slack returned an empty userinfo response.");

        if (!userInfo.Ok)
        {
            logger.LogWarning("Slack userinfo returned error: {Error}", userInfo.Error);
            throw new InvalidOperationException("Slack userinfo request failed.");
        }

        return new ExternalUserInfo(
            userInfo.Sub ?? throw new InvalidOperationException("Slack userinfo missing 'sub' claim."),
            userInfo.Email ?? throw new InvalidOperationException("Slack userinfo missing 'email' claim."),
            userInfo.EmailVerified,
            userInfo.GivenName,
            userInfo.FamilyName);
    }

    private sealed class SlackTokenResponse
    {
        [JsonPropertyName("ok")]
        public bool Ok { get; init; }

        [JsonPropertyName("access_token")]
        public string? AccessToken { get; init; }

        [JsonPropertyName("error")]
        public string? Error { get; init; }
    }

    private sealed class SlackUserInfo
    {
        [JsonPropertyName("ok")]
        public bool Ok { get; init; }

        [JsonPropertyName("error")]
        public string? Error { get; init; }

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

    /// <summary>
    /// Truncates provider error response bodies to prevent PII leakage in logs.
    /// </summary>
    private static string Truncate(string value, int maxLength = 200) =>
        value.Length <= maxLength ? value : value[..maxLength] + "...[truncated]";
}
