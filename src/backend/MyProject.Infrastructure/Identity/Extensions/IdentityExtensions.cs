using Microsoft.Extensions.DependencyInjection;
using MyProject.Application.Identity;

namespace MyProject.Infrastructure.Identity.Extensions;

public static class IdentityExtensions
{
    public static IServiceCollection AddUserContext(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IUserContext, UserContext>();
        return services;
    }
}
