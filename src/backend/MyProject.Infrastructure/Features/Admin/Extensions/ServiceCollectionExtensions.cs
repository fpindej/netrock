using Microsoft.Extensions.DependencyInjection;
using MyProject.Application.Features.Admin;
using MyProject.Infrastructure.Features.Admin.Services;

namespace MyProject.Infrastructure.Features.Admin.Extensions;

/// <summary>
/// Extension methods for registering admin feature services.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the admin services for user and role management.
        /// </summary>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddAdminServices()
        {
            services.AddScoped<IAdminService, AdminService>();
            services.AddScoped<IRoleManagementService, RoleManagementService>();
            return services;
        }
    }
}
