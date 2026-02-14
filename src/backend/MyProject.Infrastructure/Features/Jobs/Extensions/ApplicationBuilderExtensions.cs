using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MyProject.Infrastructure.Features.Jobs.Extensions;

/// <summary>
/// Extension methods for configuring the Hangfire middleware pipeline and registering recurring jobs.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Configures the Hangfire dashboard (development only) and registers all recurring jobs
    /// discovered via <see cref="IRecurringJobDefinition"/> implementations.
    /// <para>
    /// In development, the built-in Hangfire dashboard is available at <c>/hangfire</c>
    /// with no authentication. In production, use the admin API endpoints instead.
    /// </para>
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseJobScheduling(this IApplicationBuilder app)
    {
        var env = app.ApplicationServices.GetRequiredService<IHostEnvironment>();

        if (env.IsDevelopment())
        {
            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = []
            });
        }

        RegisterRecurringJobs(app.ApplicationServices);

        return app;
    }

    private static void RegisterRecurringJobs(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var jobDefinitions = scope.ServiceProvider.GetServices<IRecurringJobDefinition>();

        foreach (var job in jobDefinitions)
        {
            RecurringJob.AddOrUpdate(
                job.JobId,
                () => ExecuteJobAsync(serviceProvider, job.JobId),
                job.CronExpression);
        }
    }

    /// <summary>
    /// Resolves a job definition from DI and executes it.
    /// This indirection ensures each execution gets a fresh DI scope with proper lifetime management.
    /// </summary>
    /// <param name="serviceProvider">The root service provider.</param>
    /// <param name="jobId">The job identifier to resolve and execute.</param>
    public static async Task ExecuteJobAsync(IServiceProvider serviceProvider, string jobId)
    {
        using var scope = serviceProvider.CreateScope();
        var jobDefinitions = scope.ServiceProvider.GetServices<IRecurringJobDefinition>();
        var job = jobDefinitions.FirstOrDefault(j => j.JobId == jobId);

        if (job is not null)
        {
            await job.ExecuteAsync();
        }
    }
}
