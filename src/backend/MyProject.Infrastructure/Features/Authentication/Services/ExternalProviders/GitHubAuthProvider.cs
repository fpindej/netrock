using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Web;
using Microsoft.Extensions.Logging;
using MyProject.Infrastructure.Features.Authentication.Options;

namespace MyProject.Infrastructure.Features.Authentication.Services.ExternalProviders;

/// <summary>
/// GitHub OAuth2 authentication provider.
/// Exchanges authorization codes via the token endpoint, then calls <c>/user</c> and <c>/user/emails</c>
/// to retrieve the user's identity and primary verified email.
/// </summary>
internal sealed class GitHubAuthProvider(
    IHttpClientFactory httpClientFactory,
    ExternalAuthOptions.ProviderOptions options,
    ILogger<GitHubAuthProvider> logger) : IExternalAuthProvider
{
    private const string AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
    private const string TokenEndpoint = "https://github.com/login/oauth/access_token";
    private const string UserEndpoint = "https://api.github.com/user";
    private const string UserEmailsEndpoint = "https://api.github.com/user/emails";
    private const string HttpClientName = "GitHub-OAuth";

    /// <inheritdoc />
    public string Name => "GitHub";

    /// <inheritdoc />
    public string DisplayName => "GitHub";

    /// <inheritdoc />
    public string BuildAuthorizationUrl(string state, string redirectUri, string? nonce = null)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        query["client_id"] = options.ClientId;
        query["redirect_uri"] = redirectUri;
        query["scope"] = "read:user user:email";
        query["state"] = state;

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
            ["redirect_uri"] = redirectUri
        });

        using var tokenRequestMessage = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint)
        {
            Content = tokenRequest
        };
        tokenRequestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var tokenResponse = await httpClient.SendAsync(tokenRequestMessage, cancellationToken);

        if (!tokenResponse.IsSuccessStatusCode)
        {
            var errorBody = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("GitHub token exchange failed with {StatusCode}: {Body}",
                tokenResponse.StatusCode, errorBody);
            throw new InvalidOperationException("GitHub token exchange failed.");
        }

        var tokenResult = await tokenResponse.Content.ReadFromJsonAsync<GitHubTokenResponse>(cancellationToken)
            ?? throw new InvalidOperationException("GitHub returned an empty token response.");

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

    private static async Task<GitHubUser> GetUserAsync(
        HttpClient httpClient, string accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, UserEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("MyProject", "1.0"));

        var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<GitHubUser>(cancellationToken)
            ?? throw new InvalidOperationException("GitHub returned an empty user response.");
    }

    private static async Task<(string Email, bool Verified)> GetPrimaryEmailAsync(
        HttpClient httpClient, string accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, UserEmailsEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("MyProject", "1.0"));

        var response = await httpClient.SendAsync(request, cancellationToken);
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
}
