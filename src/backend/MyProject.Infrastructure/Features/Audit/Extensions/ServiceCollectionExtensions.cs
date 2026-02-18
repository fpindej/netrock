using Microsoft.Extensions.DependencyInjection;
using MyProject.Application.Features.Audit;
using MyProject.Infrastructure.Features.Audit.Services;

namespace MyProject.Infrastructure.Features.Audit.Extensions;

/// <summary>
/// Extension methods for registering audit feature services.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the audit services for event logging and retrieval.
        /// </summary>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddAuditServices()
        {
            services.AddScoped<IAuditService, AuditService>();
            return services;
        }
    }
}
