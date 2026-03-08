using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using Microsoft.Extensions.Logging;
using Test.Shared;

namespace Test.Infrastructure.Features.Authentication.Services.ExternalProviders;

/// <summary>
/// Apple Sign In (OIDC) authentication provider.
/// Exchanges authorization codes via the token endpoint, then decodes the <c>id_token</c>
/// payload (without signature verification - Apple's token endpoint is the trusted issuer)
/// to extract the user's identity claims.
/// </summary>
internal sealed class AppleAuthProvider(
    IHttpClientFactory httpClientFactory,
    ILogger<AppleAuthProvider> logger) : IExternalAuthProvider
{
    private const string AuthorizationEndpoint = "https://appleid.apple.com/auth/authorize";
    private const string TokenEndpoint = "https://appleid.apple.com/auth/token";
    internal const string HttpClientName = "Apple-OAuth";

    /// <inheritdoc />
    public string Name => "Apple";

    /// <inheritdoc />
    public string DisplayName => "Apple";

    /// <inheritdoc />
    public string BuildAuthorizationUrl(ProviderCredentials credentials, string state, string redirectUri, string? nonce = null)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        query["client_id"] = credentials.ClientId;
        query["redirect_uri"] = redirectUri;
        query["response_type"] = "code";
        query["scope"] = "name email";
        query["response_mode"] = "query";
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
            logger.LogWarning("Apple token exchange failed with {StatusCode}: {Body}",
                tokenResponse.StatusCode, Truncate(errorBody));
            throw new InvalidOperationException("Apple token exchange failed.");
        }

        var tokenResult = await tokenResponse.Content.ReadFromJsonAsync<AppleTokenResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Apple returned an empty token response.");

        if (string.IsNullOrEmpty(tokenResult.IdToken))
        {
            throw new InvalidOperationException("Apple token response did not contain an id_token.");
        }

        return DecodeIdToken(tokenResult.IdToken);
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
            logger.LogWarning(ex, "Apple test connection failed");
            return Result.Failure(ErrorMessages.ExternalAuth.TestConnectionProviderUnreachable);
        }
    }

    /// <summary>
    /// Decodes the id_token payload from Apple's token endpoint.
    /// Since we received the token directly from Apple over HTTPS (not from a client),
    /// the token endpoint is the trusted source and cryptographic verification is not required.
    /// </summary>
    private static ExternalUserInfo DecodeIdToken(string idToken)
    {
        var parts = idToken.Split('.');
        if (parts.Length < 2)
        {
            throw new InvalidOperationException("Apple id_token has an invalid format.");
        }

        var payload = parts[1];
        // Pad base64url to standard base64
        payload = payload.Replace('-', '+').Replace('_', '/');
        switch (payload.Length % 4)
        {
            case 2: payload += "=="; break;
            case 3: payload += "="; break;
        }

        var bytes = Convert.FromBase64String(payload);
        var claims = System.Text.Json.JsonSerializer.Deserialize<AppleIdTokenClaims>(bytes)
            ?? throw new InvalidOperationException("Apple id_token payload could not be deserialized.");

        return new ExternalUserInfo(
            claims.Sub ?? throw new InvalidOperationException("Apple id_token missing 'sub' claim."),
            claims.Email ?? throw new InvalidOperationException("Apple id_token missing 'email' claim."),
            claims.EmailVerified,
            null,
            null);
    }

    private sealed class AppleTokenResponse
    {
        [JsonPropertyName("id_token")]
        public string? IdToken { get; init; }
    }

    private sealed class TokenErrorResponse
    {
        [JsonPropertyName("error")]
        public string? Error { get; init; }
    }

    private sealed class AppleIdTokenClaims
    {
        [JsonPropertyName("sub")]
        public string? Sub { get; init; }

        [JsonPropertyName("email")]
        public string? Email { get; init; }

        [JsonPropertyName("email_verified")]
        public JsonElement? EmailVerifiedRaw { get; init; }

        /// <summary>
        /// Apple sends email_verified as either a boolean or the string "true"/"false".
        /// </summary>
        public bool EmailVerified => EmailVerifiedRaw?.ValueKind switch
        {
            System.Text.Json.JsonValueKind.True => true,
            System.Text.Json.JsonValueKind.False => false,
            System.Text.Json.JsonValueKind.String => string.Equals(
                EmailVerifiedRaw?.GetString(), "true", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    /// <summary>
    /// Truncates provider error response bodies to prevent PII leakage in logs.
    /// </summary>
    private static string Truncate(string value, int maxLength = 200) =>
        value.Length <= maxLength ? value : value[..maxLength] + "...[truncated]";
}
