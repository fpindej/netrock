using Hangfire;
using Microsoft.Extensions.Logging;
using MyProject.Application.Features.Email;
using MyProject.Infrastructure.Features.Email.Jobs;

namespace MyProject.Infrastructure.Features.Email.Services;

/// <summary>
/// Queues emails as Hangfire background jobs instead of sending them inline.
/// Delivery is performed by <see cref="EmailDeliveryJob"/> with automatic retries,
/// so a transient SMTP outage delays the email rather than losing it.
/// Registered when both <c>Email:Enabled</c> and <c>JobScheduling:Enabled</c> are <c>true</c>.
/// </summary>
internal class BackgroundEmailService(
    IBackgroundJobClient backgroundJobClient,
    ILogger<BackgroundEmailService> logger) : IEmailService
{
    /// <inheritdoc />
    public Task SendEmailAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var jobId = backgroundJobClient.Enqueue<EmailDeliveryJob>(job =>
            job.ExecuteAsync(message, CancellationToken.None));

        logger.LogInformation("Email to {To} queued as job {JobId} | Subject: {Subject}",
            message.To, jobId, message.Subject);

        return Task.CompletedTask;
    }
}
