using Microsoft.Extensions.DependencyInjection;
using MyProject.Application.Cookies;

namespace MyProject.Infrastructure.Cookies.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCookieServices(this IServiceCollection services)
    {
        services.AddScoped<ICookieService, CookieService>();
        return services;
    }
}
