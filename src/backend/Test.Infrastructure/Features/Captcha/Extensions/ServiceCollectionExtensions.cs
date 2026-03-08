using Microsoft.Extensions.DependencyInjection;
using Test.Application.Features.Captcha;
using Test.Infrastructure.Features.Captcha.Options;
using Test.Infrastructure.Features.Captcha.Services;

namespace Test.Infrastructure.Features.Captcha.Extensions;

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
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddCaptchaServices()
        {
            services.AddHttpContextAccessor();
            services.AddOptions<CaptchaOptions>()
                .BindConfiguration(CaptchaOptions.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddHttpClient<ICaptchaService, TurnstileCaptchaService>();
            return services;
        }
    }
}
