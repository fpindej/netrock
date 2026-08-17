using Hangfire;
using MyProject.Application.Features.Email;
using MyProject.Infrastructure.Features.Email.Services;

namespace MyProject.Infrastructure.Features.Email.Jobs;

/// <summary>
/// Hangfire job that delivers a single rendered email via SMTP.
/// Enqueued by <see cref="BackgroundEmailService"/> so that transient SMTP failures
/// are retried automatically instead of being silently lost. Failed deliveries remain
/// visible in the Hangfire dashboard, where they can be retried manually.
/// </summary>
internal sealed class EmailDeliveryJob(SmtpEmailService smtpEmailService)
{
    /// <summary>
    /// Maximum number of automatic retries after the first failed attempt.
    /// </summary>
    public const int RetryAttempts = 5;

    /// <summary>
    /// Sends the given message via SMTP. Any delivery exception is propagated so Hangfire
    /// records the failure (with the exception details) and schedules the next retry.
    /// <para>
    /// <paramref name="message"/> is JSON-serialized by Hangfire and persisted with the job;
    /// the <see cref="CancellationToken"/> is injected by Hangfire and signals server shutdown.
    /// </para>
    /// </summary>
    /// <param name="message">The rendered email message to deliver.</param>
    /// <param name="cancellationToken">Hangfire-injected token signalling server shutdown.</param>
    // Backoff between retries: 30s, 2m, 10m, 30m, 1h (roughly 1.7 hours in total).
    [AutomaticRetry(Attempts = RetryAttempts, DelaysInSeconds = [30, 120, 600, 1800, 3600],
        OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    public Task ExecuteAsync(EmailMessage message, CancellationToken cancellationToken) =>
        smtpEmailService.SendEmailAsync(message, cancellationToken);
}
