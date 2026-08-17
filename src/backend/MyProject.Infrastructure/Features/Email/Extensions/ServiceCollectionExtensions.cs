using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyProject.Application.Features.Email;
using MyProject.Infrastructure.Features.Email.Jobs;
using MyProject.Infrastructure.Features.Email.Options;
using MyProject.Infrastructure.Features.Email.Services;
using MyProject.Infrastructure.Features.Jobs.Options;

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
        /// <list type="bullet">
        /// <item><c>Email:Enabled</c> and <c>JobScheduling:Enabled</c> both <c>true</c>:
        /// <see cref="BackgroundEmailService"/> queues sends as Hangfire jobs executed by
        /// <see cref="EmailDeliveryJob"/> (SMTP with automatic retries).</item>
        /// <item><c>Email:Enabled</c> only: <see cref="SmtpEmailService"/> sends inline.</item>
        /// <item><c>Email:Enabled</c> is <c>false</c>: <see cref="NoOpEmailService"/> (log only).</item>
        /// </list>
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

            var jobSchedulingEnabled = configuration
                .GetSection(JobSchedulingOptions.SectionName)
                .Get<JobSchedulingOptions>()?.Enabled ?? new JobSchedulingOptions().Enabled;

            if (options.Enabled && jobSchedulingEnabled)
            {
                services.AddScoped<SmtpEmailService>();
                services.AddScoped<EmailDeliveryJob>();
                services.AddScoped<IEmailService, BackgroundEmailService>();
            }
            else if (options.Enabled)
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
