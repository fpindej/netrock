namespace MyProject.Application.Features.Email;

/// <summary>
/// Renders email templates into subject, HTML body, and optional plain text body.
/// </summary>
public interface IEmailTemplateRenderer
{
    /// <summary>
    /// Renders the named email template with the given model.
    /// </summary>
    /// <typeparam name="TModel">The model type whose properties are exposed as template variables.</typeparam>
    /// <param name="templateName">The template name (e.g. "verify-email") — without file extension.</param>
    /// <param name="model">The model instance to bind into the template context.</param>
    /// <returns>A <see cref="RenderedEmail"/> containing subject, HTML body, and optional plain text body.</returns>
    RenderedEmail Render<TModel>(string templateName, TModel model) where TModel : class;
}

/// <summary>
/// The result of rendering an email template.
/// </summary>
/// <param name="Subject">The rendered email subject line.</param>
/// <param name="HtmlBody">The rendered HTML body (wrapped in the shared base layout).</param>
/// <param name="PlainTextBody">The rendered plain text body, or null if no plain text template exists.</param>
public record RenderedEmail(string Subject, string HtmlBody, string? PlainTextBody = null);
