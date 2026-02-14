using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyProject.Application.Features.Jobs;
using MyProject.Infrastructure.Features.Jobs.RecurringJobs;
using MyProject.Infrastructure.Features.Jobs.Services;

namespace MyProject.Infrastructure.Features.Jobs.Extensions;

/// <summary>
/// Extension methods for registering Hangfire job scheduling services.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers Hangfire with PostgreSQL storage and all job-related services.
        /// <para>
        /// Uses the same <c>ConnectionStrings:Database</c> connection string as the main database.
        /// Hangfire will automatically create its schema in a <c>hangfire</c> schema on first startup.
        /// </para>
        /// </summary>
        /// <param name="configuration">The application configuration for reading connection strings.</param>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddJobScheduling(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("Database");

            services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(options =>
                    options.UseNpgsqlConnection(connectionString)));

            services.AddHangfireServer(options =>
            {
                options.WorkerCount = Environment.ProcessorCount;
                options.ServerTimeout = TimeSpan.FromMinutes(5);
                options.ShutdownTimeout = TimeSpan.FromSeconds(30);
            });

            // Register recurring job definitions — add new jobs here.
            services.AddScoped<ExpiredRefreshTokenCleanupJob>();
            services.AddScoped<IRecurringJobDefinition>(sp =>
                sp.GetRequiredService<ExpiredRefreshTokenCleanupJob>());

            services.AddScoped<IJobManagementService, JobManagementService>();

            return services;
        }
    }
}
