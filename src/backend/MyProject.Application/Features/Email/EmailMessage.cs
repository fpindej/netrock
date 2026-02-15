namespace MyProject.Application.Features.Email;

/// <summary>
/// Represents an email message to be sent by the email service.
/// </summary>
/// <param name="To">The recipient email address.</param>
/// <param name="Subject">The email subject line.</param>
/// <param name="HtmlBody">The HTML body of the email.</param>
/// <param name="PlainTextBody">Optional plain text body as a fallback for clients that do not support HTML.</param>
public record EmailMessage(
    string To,
    string Subject,
    string HtmlBody,
    string? PlainTextBody = null
);
