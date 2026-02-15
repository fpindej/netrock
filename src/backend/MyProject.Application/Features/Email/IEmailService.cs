namespace MyProject.Application.Features.Email;

/// <summary>
/// Provides email sending capabilities. Implementations may send via SMTP, a third-party API, or log-only (NoOp).
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email message asynchronously.
    /// </summary>
    /// <param name="message">The email message to send.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous send operation.</returns>
    Task SendEmailAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
