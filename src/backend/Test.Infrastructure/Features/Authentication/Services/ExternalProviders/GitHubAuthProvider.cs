using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Web;
using Microsoft.Extensions.Logging;
using Test.Shared;

namespace Test.Infrastructure.Features.Authentication.Services.ExternalProviders;

/// <summary>
/// GitHub OAuth2 authentication provider.
/// Exchanges authorization codes via the token endpoint, then calls <c>/user</c> and <c>/user/emails</c>
/// to retrieve the user's identity and primary verified email.
/// </summary>
internal sealed class GitHubAuthProvider(
    IHttpClientFactory httpClientFactory,
    ILogger<GitHubAuthProvider> logger) : IExternalAuthProvider
{
    private const string AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
    private const string TokenEndpoint = "https://github.com/login/oauth/access_token";
    private const string UserEndpoint = "https://api.github.com/user";
    private const string UserEmailsEndpoint = "https://api.github.com/user/emails";
    internal const string HttpClientName = "GitHub-OAuth";

    /// <inheritdoc />
    public string Name => "GitHub";

    /// <inheritdoc />
    public string DisplayName => "GitHub";

    /// <inheritdoc />
    public string BuildAuthorizationUrl(ProviderCredentials credentials, string state, string redirectUri, string? nonce = null)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        query["client_id"] = credentials.ClientId;
        query["redirect_uri"] = redirectUri;
        query["scope"] = "read:user user:email";
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

        using var tokenRequestMessage = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint)
        {
            Content = tokenRequest
        };
        tokenRequestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var tokenResponse = await httpClient.SendAsync(tokenRequestMessage, cancellationToken);

        if (!tokenResponse.IsSuccessStatusCode)
        {
            var errorBody = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning("GitHub token exchange failed with {StatusCode}: {Body}",
                tokenResponse.StatusCode, Truncate(errorBody));
            throw new InvalidOperationException("GitHub token exchange failed.");
        }

        var tokenResult = await tokenResponse.Content.ReadFromJsonAsync<GitHubTokenResponse>(cancellationToken)
            ?? throw new InvalidOperationException("GitHub returned an empty token response.");

        if (!string.IsNullOrEmpty(tokenResult.Error))
        {
            logger.LogWarning("GitHub token exchange returned error: {Error} - {Description}",
                tokenResult.Error, tokenResult.ErrorDescription);
            throw new InvalidOperationException("GitHub token exchange failed.");
        }

        if (string.IsNullOrEmpty(tokenResult.AccessToken))
        {
            throw new InvalidOperationException("GitHub token response did not contain an access_token.");
        }

        var user = await GetUserAsync(httpClient, tokenResult.AccessToken, cancellationToken);
        var (email, emailVerified) = await GetPrimaryEmailAsync(httpClient, tokenResult.AccessToken, cancellationToken);

        return new ExternalUserInfo(
            user.Id.ToString(),
            email,
            emailVerified,
            ExtractFirstName(user.Name),
            ExtractLastName(user.Name));
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
                ["client_secret"] = credentials.ClientSecret
            });

            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint)
            {
                Content = tokenRequest
            };
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await httpClient.SendAsync(requestMessage, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return Result.Failure(ErrorMessages.ExternalAuth.TestConnectionInvalidCredentials);
            }

            // GitHub returns 200 even on errors - check the error field in the JSON body
            var body = await response.Content.ReadFromJsonAsync<GitHubTokenResponse>(cancellationToken);
            if (body?.Error is "bad_verification_code")
            {
                return Result.Success();
            }

            if (body?.Error is "incorrect_client_credentials")
            {
                return Result.Failure(ErrorMessages.ExternalAuth.TestConnectionInvalidCredentials);
            }

            return Result.Success();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "GitHub test connection failed");
            return Result.Failure(ErrorMessages.ExternalAuth.TestConnectionProviderUnreachable);
        }
    }

    private static async Task<GitHubUser> GetUserAsync(
        HttpClient httpClient, string accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, UserEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("Test", "1.0"));

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<GitHubUser>(cancellationToken)
            ?? throw new InvalidOperationException("GitHub returned an empty user response.");
    }

    private static async Task<(string Email, bool Verified)> GetPrimaryEmailAsync(
        HttpClient httpClient, string accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, UserEmailsEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("Test", "1.0"));

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var emails = await response.Content.ReadFromJsonAsync<List<GitHubEmail>>(cancellationToken)
            ?? throw new InvalidOperationException("GitHub returned an empty emails response.");

        var primary = emails.FirstOrDefault(e => e.IsPrimary && e.IsVerified)
            ?? emails.FirstOrDefault(e => e.IsPrimary)
            ?? emails.FirstOrDefault(e => e.IsVerified)
            ?? throw new InvalidOperationException("GitHub user has no usable email address.");

        return (primary.Email, primary.IsVerified);
    }

    private static string? ExtractFirstName(string? fullName) =>
        fullName?.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

    private static string? ExtractLastName(string? fullName)
    {
        if (fullName is null) return null;
        var parts = fullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 1 ? parts[1] : null;
    }

    private sealed class GitHubTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; init; }

        [JsonPropertyName("error")]
        public string? Error { get; init; }

        [JsonPropertyName("error_description")]
        public string? ErrorDescription { get; init; }
    }

    private sealed class GitHubUser
    {
        [JsonPropertyName("id")]
        public long Id { get; init; }

        [JsonPropertyName("name")]
        public string? Name { get; init; }
    }

    private sealed class GitHubEmail
    {
        [JsonPropertyName("email")]
        public string Email { get; init; } = string.Empty;

        [JsonPropertyName("primary")]
        public bool IsPrimary { get; init; }

        [JsonPropertyName("verified")]
        public bool IsVerified { get; init; }
    }

    /// <summary>
    /// Truncates provider error response bodies to prevent PII leakage in logs.
    /// </summary>
    private static string Truncate(string value, int maxLength = 200) =>
        value.Length <= maxLength ? value : value[..maxLength] + "...[truncated]";
}
