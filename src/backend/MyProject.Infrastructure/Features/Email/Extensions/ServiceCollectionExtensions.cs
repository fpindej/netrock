using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyProject.Application.Features.Email;
using MyProject.Infrastructure.Features.Email.Options;
using MyProject.Infrastructure.Features.Email.Services;

namespace MyProject.Infrastructure.Features.Email.Extensions;

/// <summary>
/// Extension methods for registering email services and configuration.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers email options, the template rendering pipeline, and the email service.
        /// When <c>Email:Enabled</c> is <c>true</c>, registers <see cref="SmtpEmailService"/>;
        /// otherwise registers <see cref="NoOpEmailService"/> (log only).
        /// </summary>
        /// <param name="configuration">The application configuration for reading email options.</param>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddEmailServices(IConfiguration configuration)
        {
            services.AddOptions<EmailOptions>()
                .BindConfiguration(EmailOptions.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            var options = configuration
                .GetSection(EmailOptions.SectionName)
                .Get<EmailOptions>() ?? new EmailOptions();

            if (options.Enabled)
            {
                services.AddScoped<IEmailService, SmtpEmailService>();
            }
            else
            {
                services.AddScoped<IEmailService, NoOpEmailService>();
            }

            services.AddSingleton<IEmailTemplateRenderer, FluidEmailTemplateRenderer>();
            services.AddScoped<ITemplatedEmailSender, TemplatedEmailSender>();

            return services;
        }
    }
}
