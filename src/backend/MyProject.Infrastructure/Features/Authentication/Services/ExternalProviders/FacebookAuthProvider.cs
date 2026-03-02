using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Web;
using Microsoft.Extensions.Logging;
using MyProject.Shared;

namespace MyProject.Infrastructure.Features.Authentication.Services.ExternalProviders;

/// <summary>
/// Facebook OAuth2 authentication provider.
/// Exchanges authorization codes via the Graph API token endpoint, then calls the
/// <c>/me</c> endpoint with the access token to retrieve user identity.
/// Facebook only returns verified emails, so <c>email_verified</c> is always <c>true</c>.
/// </summary>
internal sealed class FacebookAuthProvider(
    IHttpClientFactory httpClientFactory,
    ILogger<FacebookAuthProvider> logger) : IExternalAuthProvider
{
    private const string AuthorizationEndpoint = "https://www.facebook.com/v21.0/dialog/oauth";
    private const string TokenEndpoint = "https://graph.facebook.com/v21.0/oauth/access_token";
    private const string UserInfoEndpoint = "https://graph.facebook.com/v21.0/me?fields=id,email,first_name,last_name";
    internal const string HttpClientName = "Facebook-OAuth";

    /// <inheritdoc />
    public string Name => "Facebook";

    /// <inheritdoc />
    public string DisplayName => "Facebook";

    /// <inheritdoc />
    public string BuildAuthorizationUrl(ProviderCredentials credentials, string state, string redirectUri, string? nonce = null)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        query["client_id"] = credentials.ClientId;
        query["redirect_uri"] = redirectUri;
        query["response_type"] = "code";
        query["scope"] = "email public_profile";
        query["state"] = state;

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
            logger.LogWarning("Facebook token exchange failed with {StatusCode}: {Body}",
                tokenResponse.StatusCode, Truncate(errorBody));
            throw new InvalidOperationException("Facebook token exchange failed.");
        }

        var tokenResult = await tokenResponse.Content.ReadFromJsonAsync<FacebookTokenResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Facebook returned an empty token response.");

        if (string.IsNullOrEmpty(tokenResult.AccessToken))
        {
            throw new InvalidOperationException("Facebook token response did not contain an access_token.");
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

            var body = await response.Content.ReadFromJsonAsync<FacebookErrorEnvelope>(cancellationToken);
            if (body?.Error?.Type is "OAuthException" && body.Error.Code is 101)
            {
                return Result.Failure(ErrorMessages.ExternalAuth.TestConnectionInvalidCredentials);
            }

            return Result.Success();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Facebook test connection failed");
            return Result.Failure(ErrorMessages.ExternalAuth.TestConnectionProviderUnreachable);
        }
    }

    /// <summary>
    /// Calls the Facebook Graph API /me endpoint to retrieve user identity.
    /// Facebook only provides verified emails, so email_verified is always true.
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
            logger.LogWarning("Facebook userinfo request failed with {StatusCode}: {Body}",
                response.StatusCode, Truncate(errorBody));
            throw new InvalidOperationException("Facebook userinfo request failed.");
        }

        var userInfo = await response.Content.ReadFromJsonAsync<FacebookUserInfo>(cancellationToken)
            ?? throw new InvalidOperationException("Facebook returned an empty userinfo response.");

        return new ExternalUserInfo(
            userInfo.Id ?? throw new InvalidOperationException("Facebook userinfo missing 'id' field."),
            userInfo.Email ?? throw new InvalidOperationException("Facebook userinfo missing 'email' field."),
            true,
            userInfo.FirstName,
            userInfo.LastName);
    }

    private sealed class FacebookTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; init; }
    }

    private sealed class FacebookErrorEnvelope
    {
        [JsonPropertyName("error")]
        public FacebookError? Error { get; init; }
    }

    private sealed class FacebookError
    {
        [JsonPropertyName("type")]
        public string? Type { get; init; }

        [JsonPropertyName("code")]
        public int Code { get; init; }
    }

    private sealed class FacebookUserInfo
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("email")]
        public string? Email { get; init; }

        [JsonPropertyName("first_name")]
        public string? FirstName { get; init; }

        [JsonPropertyName("last_name")]
        public string? LastName { get; init; }
    }

    /// <summary>
    /// Truncates provider error response bodies to prevent PII leakage in logs.
    /// </summary>
    private static string Truncate(string value, int maxLength = 200) =>
        value.Length <= maxLength ? value : value[..maxLength] + "...[truncated]";
}
