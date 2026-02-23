using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Encodings.Web;
using Fluid;
using Microsoft.Extensions.Options;
using MyProject.Application.Features.Email;
using MyProject.Application.Features.Email.Models;
using MyProject.Infrastructure.Features.Email.Options;

namespace MyProject.Infrastructure.Features.Email.Services;

/// <summary>
/// Renders Liquid email templates from embedded resources using the Fluid template engine.
/// Templates are compiled once and cached for the lifetime of the application.
/// Thread-safe: each parse creates a new <see cref="FluidParser"/> and
/// <see cref="Lazy{T}"/> guarantees single-execution per cache key.
/// </summary>
internal class FluidEmailTemplateRenderer : IEmailTemplateRenderer
{
    private static readonly Assembly ResourceAssembly = typeof(FluidEmailTemplateRenderer).Assembly;
    private static readonly string ResourcePrefix =
        typeof(FluidEmailTemplateRenderer).Namespace!.Replace(".Services", ".Templates");

    private readonly ConcurrentDictionary<string, Lazy<IFluidTemplate>> _cache = new();
    private readonly TemplateOptions _options;
    private readonly string _appName;

    /// <summary>
    /// Initializes a new instance of <see cref="FluidEmailTemplateRenderer"/>.
    /// </summary>
    /// <param name="emailOptions">Email options providing the application display name.</param>
    public FluidEmailTemplateRenderer(IOptions<EmailOptions> emailOptions)
    {
        _appName = emailOptions.Value.FromName;
        _options = CreateOptions();
    }

    /// <inheritdoc />
    public RenderedEmail Render<TModel>(string templateName, TModel model) where TModel : class
    {
        // Subject: rendered without HTML encoding
        var subject = RenderTemplate($"{templateName}.subject", model, encoder: null).Trim();

        // Child HTML body fragment: rendered with HTML encoding
        var childHtml = RenderTemplate(templateName, model, HtmlEncoder.Default);

        // Wrap in base layout (also HTML-encoded)
        var htmlBody = RenderBaseLayout(childHtml);

        // Plain text: rendered without any encoding
        string? plainTextBody = null;
        var textTemplate = FindTemplate($"{templateName}.text");
        if (textTemplate is not null)
        {
            var context = CreateContext(model);
            plainTextBody = textTemplate.Render(context).Trim();
        }

        return new RenderedEmail(subject, htmlBody, plainTextBody);
    }

    /// <summary>
    /// Renders the shared base layout with the given child HTML injected as the <c>body</c> variable.
    /// The <c>body</c> content is already HTML so the <c>raw</c> filter in the template prevents double-encoding.
    /// </summary>
    private string RenderBaseLayout(string childHtml)
    {
        var baseTemplate = GetOrParseTemplate("_base");
        var context = new TemplateContext(_options);
        context.SetValue("body", childHtml);
        context.SetValue("app_name", _appName);
        return baseTemplate.Render(context, HtmlEncoder.Default);
    }

    /// <summary>
    /// Renders a single template file with the given model and optional encoder.
    /// When <paramref name="encoder"/> is null, no encoding is applied (subject/plain text).
    /// </summary>
    private string RenderTemplate<TModel>(string templateName, TModel model, TextEncoder? encoder) where TModel : class
    {
        var template = GetOrParseTemplate(templateName);
        var context = CreateContext(model);
        return encoder is not null
            ? template.Render(context, encoder)
            : template.Render(context);
    }

    /// <summary>
    /// Creates a <see cref="TemplateContext"/> populated with the model and the <c>app_name</c> variable.
    /// </summary>
    private TemplateContext CreateContext<TModel>(TModel model) where TModel : class
    {
        var context = new TemplateContext(model, _options);
        context.SetValue("app_name", _appName);
        return context;
    }

    /// <summary>
    /// Returns a cached compiled template, parsing the embedded resource on first access.
    /// Uses <see cref="Lazy{T}"/> to guarantee the parse factory runs exactly once per key,
    /// even under concurrent access.
    /// </summary>
    private IFluidTemplate GetOrParseTemplate(string templateName)
    {
        var lazy = _cache.GetOrAdd(templateName, static name => new Lazy<IFluidTemplate>(() =>
        {
            var source = ReadResource(name);
            var parser = new FluidParser();
            if (!parser.TryParse(source, out var template, out var error))
            {
                throw new InvalidOperationException($"Failed to parse Liquid template '{name}': {error}");
            }
            return template;
        }));

        return lazy.Value;
    }

    /// <summary>
    /// Returns a cached compiled template if the embedded resource exists, or null if absent.
    /// Used for optional template variants (e.g. plain text companions).
    /// </summary>
    private IFluidTemplate? FindTemplate(string templateName)
    {
        var resourceName = $"{ResourcePrefix}.{templateName}.liquid";
        if (ResourceAssembly.GetManifestResourceInfo(resourceName) is null)
        {
            return null;
        }

        return GetOrParseTemplate(templateName);
    }

    /// <summary>
    /// Reads an embedded <c>.liquid</c> resource by template name.
    /// </summary>
    private static string ReadResource(string templateName)
    {
        var resourceName = $"{ResourcePrefix}.{templateName}.liquid";
        using var stream = ResourceAssembly.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException($"Embedded email template '{templateName}' not found. Expected resource: {resourceName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Creates <see cref="TemplateOptions"/> with snake_case member naming and all email model types registered.
    /// </summary>
    private static TemplateOptions CreateOptions()
    {
        var options = new TemplateOptions
        {
            MemberAccessStrategy =
            {
                MemberNameStrategy = MemberNameStrategies.SnakeCase
            }
        };

        options.MemberAccessStrategy.Register<VerifyEmailModel>();
        options.MemberAccessStrategy.Register<ResetPasswordModel>();
        options.MemberAccessStrategy.Register<AdminResetPasswordModel>();
        options.MemberAccessStrategy.Register<InvitationModel>();

        return options;
    }
}
