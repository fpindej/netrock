namespace MyProject.Application.Features.Email;

/// <summary>
/// Renders an email template and sends the result, swallowing failures so
/// that transient email outages never break business operations.
/// </summary>
public interface ITemplatedEmailSender
{
    /// <summary>
    /// Renders the named template with the given model and sends the email.
    /// Both rendering and delivery failures are logged but never propagated.
    /// </summary>
    /// <typeparam name="TModel">The template model type.</typeparam>
    /// <param name="templateName">The template name (e.g. "verify-email") — without file extension.</param>
    /// <param name="model">The model instance to bind into the template context.</param>
    /// <param name="to">The recipient email address.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task SendSafeAsync<TModel>(string templateName, TModel model, string to,
        CancellationToken cancellationToken = default) where TModel : class;
}
