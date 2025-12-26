using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyProject.Application.Persistence;
using MyProject.Infrastructure.Features.Authentication.Extensions;
using MyProject.Infrastructure.Persistence.Interceptors;

namespace MyProject.Infrastructure.Persistence.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddPersistence(IConfiguration configuration)
        {
            services.ConfigureDbContext(configuration);
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped(typeof(IBaseEntityRepository<>), typeof(BaseEntityRepository<>));

            return services;
        }

        public IServiceCollection AddIdentityServices(IConfiguration configuration)
        {
            services.AddIdentity<MyProjectDbContext>(configuration);

            return services;
        }

        private IServiceCollection ConfigureDbContext(IConfiguration configuration)
        {
            services.AddScoped<AuditingInterceptor>();
            services.AddScoped<UserCacheInvalidationInterceptor>();
            services.AddDbContext<MyProjectDbContext>((sp, opt) =>
            {
                var connectionString = configuration.GetConnectionString("Database");
                opt.UseNpgsql(connectionString);
                opt.AddInterceptors(
                    sp.GetRequiredService<AuditingInterceptor>(),
                    sp.GetRequiredService<UserCacheInvalidationInterceptor>());
            });
            return services;
        }
    }
}
