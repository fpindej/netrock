using Microsoft.Extensions.Logging;
using MyProject.Application.Features.Email;

namespace MyProject.Infrastructure.Features.Email.Services;

/// <summary>
/// Renders an email template and sends the result, swallowing both rendering
/// and delivery failures. Transient provider outages (quota, auth, network)
/// and template errors are logged but never propagate to the caller.
/// <see cref="OperationCanceledException"/> is re-thrown to respect cooperative cancellation.
/// </summary>
internal class TemplatedEmailSender(
    IEmailTemplateRenderer emailTemplateRenderer,
    IEmailService emailService,
    ILogger<TemplatedEmailSender> logger) : ITemplatedEmailSender
{
    /// <inheritdoc />
    public async Task SendSafeAsync<TModel>(string templateName, TModel model, string to,
        CancellationToken cancellationToken) where TModel : class
    {
        try
        {
            var rendered = emailTemplateRenderer.Render(templateName, model);
            var message = new EmailMessage(to, rendered.Subject, rendered.HtmlBody, rendered.PlainTextBody);
            await emailService.SendEmailAsync(message, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send templated email '{TemplateName}'", templateName);
        }
    }
}
