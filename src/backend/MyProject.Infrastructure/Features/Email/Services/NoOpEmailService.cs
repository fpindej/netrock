using Microsoft.Extensions.Logging;
using MyProject.Application.Features.Email;

namespace MyProject.Infrastructure.Features.Email.Services;

/// <summary>
/// A no-op email service that logs email details to Serilog instead of sending them.
/// Intended for development and testing environments where actual email delivery is not needed.
/// </summary>
internal class NoOpEmailService(ILogger<NoOpEmailService> logger) : IEmailService
{
    /// <inheritdoc />
    public Task SendEmailAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Email to {To} | Subject: {Subject}", message.To, message.Subject);
        logger.LogDebug("Email body (HTML): {HtmlBody}", message.HtmlBody);

        if (message.PlainTextBody is not null)
        {
            logger.LogDebug("Email body (plain text): {PlainTextBody}", message.PlainTextBody);
        }

        return Task.CompletedTask;
    }
}
