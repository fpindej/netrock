namespace MyProject.Application.Features.Captcha;

/// <summary>
/// Validates CAPTCHA tokens against the verification provider.
/// </summary>
public interface ICaptchaService
{
    /// <summary>
    /// Validates a CAPTCHA token from the frontend widget.
    /// </summary>
    /// <param name="token">The CAPTCHA response token submitted by the client.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> if the token is valid; otherwise <c>false</c>.</returns>
    Task<bool> ValidateTokenAsync(string token, CancellationToken ct = default);
}
