using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyProject.Application.Features.Captcha;
using MyProject.Infrastructure.Features.Captcha.Options;
using MyProject.Infrastructure.Features.Captcha.Services;

namespace MyProject.Infrastructure.Features.Captcha.Extensions;

/// <summary>
/// Extension methods for registering CAPTCHA verification services.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers Cloudflare Turnstile CAPTCHA services and configuration.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddCaptchaServices(IConfiguration configuration)
        {
            services.AddOptions<CaptchaOptions>()
                .BindConfiguration(CaptchaOptions.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddHttpClient<ICaptchaService, TurnstileCaptchaService>();
            return services;
        }
    }
}
