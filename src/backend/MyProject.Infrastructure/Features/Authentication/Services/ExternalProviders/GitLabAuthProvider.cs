using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Web;
using Microsoft.Extensions.Logging;
using MyProject.Shared;

namespace MyProject.Infrastructure.Features.Authentication.Services.ExternalProviders;

/// <summary>
/// GitLab OIDC authentication provider.
/// Exchanges authorization codes via the token endpoint, then calls the <c>/oauth/userinfo</c> endpoint
/// with the access token to retrieve verified user identity (sub, email, name).
/// </summary>
internal sealed class GitLabAuthProvider(
    IHttpClientFactory httpClientFactory,
    ILogger<GitLabAuthProvider> logger) : IExternalAuthProvider
{
    private const string AuthorizationEndpoint = "https://gitlab.com/oauth/authorize";
    private const string TokenEndpoint = "https://gitlab.com/oauth/token";
    private const string UserInfoEndpoint = "https://gitlab.com/oauth/userinfo";
    internal const string HttpClientName = "GitLab-OAuth";

    /// <inheritdoc />
    public string Name => "GitLab";

    /// <inheritdoc />
    public string DisplayName => "GitLab";

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
            ["grant_type"] = "authorization_code"
        });

        using var tokenResponse = await httpClient.PostAsync(TokenEndpoint, tokenRequest, cancellationToken);

        if (!tokenResponse.IsSuccessStatusCode)
        {
            var errorBody = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning("GitLab token exchange failed with {StatusCode}: {Body}",
                tokenResponse.StatusCode, Truncate(errorBody));
            throw new InvalidOperationException("GitLab token exchange failed.");
        }

        var tokenResult = await tokenResponse.Content.ReadFromJsonAsync<GitLabTokenResponse>(cancellationToken)
            ?? throw new InvalidOperationException("GitLab returned an empty token response.");

        if (string.IsNullOrEmpty(tokenResult.AccessToken))
        {
            throw new InvalidOperationException("GitLab token response did not contain an access_token.");
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
            logger.LogWarning(ex, "GitLab test connection failed");
            return Result.Failure(ErrorMessages.ExternalAuth.TestConnectionProviderUnreachable);
        }
    }

    /// <summary>
    /// Calls GitLab's OIDC userinfo endpoint with the access token to retrieve verified identity claims.
    /// GitLab returns <c>name</c> as a single string, so it is split into first and last name.
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
            logger.LogWarning("GitLab userinfo request failed with {StatusCode}: {Body}",
                response.StatusCode, Truncate(errorBody));
            throw new InvalidOperationException("GitLab userinfo request failed.");
        }

        var userInfo = await response.Content.ReadFromJsonAsync<GitLabUserInfo>(cancellationToken)
            ?? throw new InvalidOperationException("GitLab returned an empty userinfo response.");

        return new ExternalUserInfo(
            userInfo.Sub ?? throw new InvalidOperationException("GitLab userinfo missing 'sub' claim."),
            userInfo.Email ?? throw new InvalidOperationException("GitLab userinfo missing 'email' claim."),
            userInfo.EmailVerified,
            ExtractFirstName(userInfo.Name),
            ExtractLastName(userInfo.Name));
    }

    private static string? ExtractFirstName(string? fullName) =>
        fullName?.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

    private static string? ExtractLastName(string? fullName)
    {
        if (fullName is null) return null;
        var parts = fullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 1 ? parts[1] : null;
    }

    private sealed class GitLabTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; init; }
    }

    private sealed class TokenErrorResponse
    {
        [JsonPropertyName("error")]
        public string? Error { get; init; }
    }

    private sealed class GitLabUserInfo
    {
        [JsonPropertyName("sub")]
        public string? Sub { get; init; }

        [JsonPropertyName("email")]
        public string? Email { get; init; }

        [JsonPropertyName("email_verified")]
        public bool EmailVerified { get; init; }

        [JsonPropertyName("name")]
        public string? Name { get; init; }
    }

    /// <summary>
    /// Truncates provider error response bodies to prevent PII leakage in logs.
    /// </summary>
    private static string Truncate(string value, int maxLength = 200) =>
        value.Length <= maxLength ? value : value[..maxLength] + "...[truncated]";
}
