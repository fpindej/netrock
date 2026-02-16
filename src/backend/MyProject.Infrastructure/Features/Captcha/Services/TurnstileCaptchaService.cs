using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyProject.Application.Features.Captcha;
using MyProject.Infrastructure.Features.Captcha.Options;

namespace MyProject.Infrastructure.Features.Captcha.Services;

/// <summary>
/// Validates CAPTCHA tokens against the Cloudflare Turnstile verification API.
/// </summary>
internal sealed class TurnstileCaptchaService(
    HttpClient httpClient,
    IOptions<CaptchaOptions> options,
    ILogger<TurnstileCaptchaService> logger) : ICaptchaService
{
    /// <inheritdoc />
    public async Task<bool> ValidateTokenAsync(string token, CancellationToken ct = default)
    {
        try
        {
            var content = new FormUrlEncodedContent(new KeyValuePair<string, string>[]
            {
                new("secret", options.Value.SecretKey),
                new("response", token)
            });

            var response = await httpClient.PostAsync(options.Value.VerifyUrl, content, ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Turnstile verification returned HTTP {StatusCode}", response.StatusCode);
                return false;
            }

            var json = await response.Content.ReadFromJsonAsync<TurnstileResponse>(ct);
            return json?.Success is true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Turnstile verification failed with exception");
            return false;
        }
    }

    /// <summary>
    /// Minimal model for the Cloudflare Turnstile siteverify response.
    /// </summary>
    private sealed class TurnstileResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; init; }
    }
}
