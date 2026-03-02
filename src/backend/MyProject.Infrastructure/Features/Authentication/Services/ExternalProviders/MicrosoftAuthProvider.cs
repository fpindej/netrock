using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Web;
using Microsoft.Extensions.Logging;
using MyProject.Shared;

namespace MyProject.Infrastructure.Features.Authentication.Services.ExternalProviders;

/// <summary>
/// Microsoft OIDC authentication provider.
/// Exchanges authorization codes via the Azure AD v2.0 token endpoint, then calls the
/// Microsoft Graph <c>/me</c> endpoint with the access token to retrieve user identity.
/// </summary>
internal sealed class MicrosoftAuthProvider(
    IHttpClientFactory httpClientFactory,
    ILogger<MicrosoftAuthProvider> logger) : IExternalAuthProvider
{
    private const string AuthorizationEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize";
    private const string TokenEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/token";
    private const string UserInfoEndpoint = "https://graph.microsoft.com/v1.0/me";
    internal const string HttpClientName = "Microsoft-OAuth";

    /// <inheritdoc />
    public string Name => "Microsoft";

    /// <inheritdoc />
    public string DisplayName => "Microsoft";

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
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code",
            ["scope"] = "openid email profile"
        });

        using var tokenResponse = await httpClient.PostAsync(TokenEndpoint, tokenRequest, cancellationToken);

        if (!tokenResponse.IsSuccessStatusCode)
        {
            var errorBody = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning("Microsoft token exchange failed with {StatusCode}: {Body}",
                tokenResponse.StatusCode, Truncate(errorBody));
            throw new InvalidOperationException("Microsoft token exchange failed.");
        }

        var tokenResult = await tokenResponse.Content.ReadFromJsonAsync<MicrosoftTokenResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Microsoft returned an empty token response.");

        if (string.IsNullOrEmpty(tokenResult.AccessToken))
        {
            throw new InvalidOperationException("Microsoft token response did not contain an access_token.");
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
            logger.LogWarning(ex, "Microsoft test connection failed");
            return Result.Failure(ErrorMessages.ExternalAuth.TestConnectionProviderUnreachable);
        }
    }

    /// <summary>
    /// Calls the Microsoft Graph /me endpoint to retrieve user identity claims.
    /// Falls back to <c>userPrincipalName</c> when <c>mail</c> is null (common for personal accounts).
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
            logger.LogWarning("Microsoft userinfo request failed with {StatusCode}: {Body}",
                response.StatusCode, Truncate(errorBody));
            throw new InvalidOperationException("Microsoft userinfo request failed.");
        }

        var userInfo = await response.Content.ReadFromJsonAsync<MicrosoftUserInfo>(cancellationToken)
            ?? throw new InvalidOperationException("Microsoft returned an empty userinfo response.");

        var email = userInfo.Mail ?? userInfo.UserPrincipalName
            ?? throw new InvalidOperationException("Microsoft userinfo missing both 'mail' and 'userPrincipalName'.");

        return new ExternalUserInfo(
            userInfo.Id ?? throw new InvalidOperationException("Microsoft userinfo missing 'id' field."),
            email,
            true,
            userInfo.GivenName,
            userInfo.Surname);
    }

    private sealed class MicrosoftTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; init; }
    }

    private sealed class TokenErrorResponse
    {
        [JsonPropertyName("error")]
        public string? Error { get; init; }
    }

    private sealed class MicrosoftUserInfo
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("mail")]
        public string? Mail { get; init; }

        [JsonPropertyName("userPrincipalName")]
        public string? UserPrincipalName { get; init; }

        [JsonPropertyName("givenName")]
        public string? GivenName { get; init; }

        [JsonPropertyName("surname")]
        public string? Surname { get; init; }
    }

    /// <summary>
    /// Truncates provider error response bodies to prevent PII leakage in logs.
    /// </summary>
    private static string Truncate(string value, int maxLength = 200) =>
        value.Length <= maxLength ? value : value[..maxLength] + "...[truncated]";
}
