using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MyProject.Application.Features.Email;
using MyProject.Infrastructure.Features.Email.Options;

namespace MyProject.Infrastructure.Features.Email.Services;

/// <summary>
/// Sends emails via SMTP using MailKit.
/// Connects, authenticates (when credentials are provided), sends, and disconnects per invocation.
/// </summary>
internal class SmtpEmailService(
    IOptions<EmailOptions> emailOptions,
    ILogger<SmtpEmailService> logger) : IEmailService
{
    /// <inheritdoc />
    public async Task SendEmailAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var options = emailOptions.Value;
        var smtp = options.Smtp;

        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(new MailboxAddress(options.FromName, options.FromAddress));
        mimeMessage.To.Add(MailboxAddress.Parse(message.To));
        mimeMessage.Subject = message.Subject;

        var builder = new BodyBuilder { HtmlBody = message.HtmlBody };

        if (message.PlainTextBody is not null)
        {
            builder.TextBody = message.PlainTextBody;
        }

        mimeMessage.Body = builder.ToMessageBody();

        using var client = new SmtpClient();

        var socketOptions = smtp.UseSsl
            ? SecureSocketOptions.StartTls
            : SecureSocketOptions.None;

        await client.ConnectAsync(smtp.Host, smtp.Port, socketOptions, cancellationToken);

        if (!string.IsNullOrEmpty(smtp.Username))
        {
            await client.AuthenticateAsync(smtp.Username, smtp.Password, cancellationToken);
        }

        await client.SendAsync(mimeMessage, cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);

        logger.LogInformation("Email sent to {To} | Subject: {Subject}", message.To, message.Subject);
    }
}
