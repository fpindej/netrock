using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyProject.Infrastructure.Features.Authentication.Extensions;

namespace MyProject.Infrastructure.Persistence.Extensions;

/// <summary>
/// Extension methods for database initialization at startup — migrations, role seeding, and development data.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Initializes the database: applies migrations (development only), seeds roles (always),
    /// and seeds test users (development only).
    /// </summary>
    /// <param name="appBuilder">The application builder.</param>
    public static async Task InitializeDatabaseAsync(this IApplicationBuilder appBuilder)
    {
        using var scope = appBuilder.ApplicationServices.CreateScope();
        var services = scope.ServiceProvider;
        var isDevelopment = services.GetRequiredService<IHostEnvironment>().IsDevelopment();

        if (isDevelopment)
        {
            ApplyMigrations(services);
        }

        await IdentitySeedExtensions.SeedRolesAsync(services);

        if (isDevelopment)
        {
            await IdentitySeedExtensions.SeedDevelopmentUsersAsync(services);
        }
    }

    private static void ApplyMigrations(IServiceProvider serviceProvider)
    {
        var dbContext = serviceProvider.GetRequiredService<MyProjectDbContext>();
        dbContext.Database.Migrate();
    }
}
