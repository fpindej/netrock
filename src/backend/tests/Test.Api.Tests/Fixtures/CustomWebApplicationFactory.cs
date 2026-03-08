using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Caching.Hybrid;
using Test.Application.Features.Admin;
using Test.Application.Features.Authentication;
using Test.Application.Features.Audit;
using Test.Application.Features.Captcha;
using Test.Application.Features.Email;
using Test.Application.Features.Jobs;
using Test.Application.Identity;
using Test.Infrastructure.Persistence;
using NSubstitute.ClearExtensions;
using IAuthenticationService = Test.Application.Features.Authentication.IAuthenticationService;
using ITwoFactorService = Test.Application.Features.Authentication.ITwoFactorService;

namespace Test.Api.Tests.Fixtures;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "TestDb_" + Guid.NewGuid();

    public IAuthenticationService AuthenticationService { get; } = Substitute.For<IAuthenticationService>();
    public IUserService UserService { get; } = Substitute.For<IUserService>();
    public IAdminService AdminService { get; } = Substitute.For<IAdminService>();
    public IRoleManagementService RoleManagementService { get; } = Substitute.For<IRoleManagementService>();
    public IJobManagementService JobManagementService { get; } = Substitute.For<IJobManagementService>();
    public IEmailService EmailService { get; } = Substitute.For<IEmailService>();
    public HybridCache HybridCache { get; } = Substitute.For<HybridCache>();
    public ICaptchaService CaptchaService { get; } = Substitute.For<ICaptchaService>();
    public IAuditService AuditService { get; } = Substitute.For<IAuditService>();
    public ITwoFactorService TwoFactorService { get; } = Substitute.For<ITwoFactorService>();
    public IExternalAuthService ExternalAuthService { get; } = Substitute.For<IExternalAuthService>();
    public IProviderConfigService ProviderConfigService { get; } = Substitute.For<IProviderConfigService>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Use Testing environment — loads appsettings.Testing.json which disables
        // Hangfire, and provides a dummy DB connection string.
        // Also avoids EF migrations and dev user seeding (non-Development).
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            // Remove Hangfire hosted services in case config override didn't prevent registration
            var hangfireDescriptors = services
                .Where(d => d.ServiceType == typeof(IHostedService) &&
                            (d.ImplementationType?.FullName?.Contains("Hangfire") == true ||
                             d.ImplementationFactory?.Method.DeclaringType?.FullName?.Contains("Hangfire") == true))
                .ToList();
            foreach (var descriptor in hangfireDescriptors)
            {
                services.Remove(descriptor);
            }

            // Remove ALL EF Core / DbContext registrations to avoid dual-provider conflict
            // (Npgsql registered by app + InMemory registered by tests)
            services.RemoveAll<DbContextOptions<TestDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<TestDbContext>();

            // Manually register InMemory options (bypasses AddDbContext's TryAdd)
            services.AddScoped(_ =>
            {
                var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();
                optionsBuilder.UseInMemoryDatabase(_dbName);
                return optionsBuilder.Options;
            });

            services.AddScoped<DbContextOptions>(sp =>
                sp.GetRequiredService<DbContextOptions<TestDbContext>>());

            services.AddScoped<TestDbContext>();

            // Replace services with mocks
            services.RemoveAll<IAuthenticationService>();
            services.AddSingleton(AuthenticationService);

            services.RemoveAll<IUserService>();
            services.AddSingleton(UserService);

            services.RemoveAll<IAdminService>();
            services.AddSingleton(AdminService);

            services.RemoveAll<IRoleManagementService>();
            services.AddSingleton(RoleManagementService);

            services.RemoveAll<IJobManagementService>();
            services.AddSingleton(JobManagementService);

            services.RemoveAll<IEmailService>();
            services.AddSingleton(EmailService);

            services.RemoveAll<HybridCache>();
            services.AddSingleton(HybridCache);

            services.RemoveAll<ICaptchaService>();
            services.AddSingleton(CaptchaService);

            services.RemoveAll<IAuditService>();
            services.AddSingleton(AuditService);

            services.RemoveAll<ITwoFactorService>();
            services.AddSingleton(TwoFactorService);

            services.RemoveAll<IExternalAuthService>();
            services.AddSingleton(ExternalAuthService);

            services.RemoveAll<IProviderConfigService>();
            services.AddSingleton(ProviderConfigService);

            // Override auth scheme — PostConfigure runs after the app's Configure,
            // ensuring the test scheme wins over the JWT Bearer defaults.
            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                options.DefaultScheme = TestAuthHandler.SchemeName;
            });

            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName, _ => { });

            // Replace TimeProvider with a fixed one
            services.RemoveAll<TimeProvider>();
            services.AddSingleton(TimeProvider.System);
        });
    }

    /// <summary>
    /// Clears all NSubstitute return values and received calls on every mock.
    /// Call from each test class constructor to prevent mock state leaking between tests.
    /// </summary>
    public void ResetMocks()
    {
        AuthenticationService.ClearSubstitute(ClearOptions.All);
        UserService.ClearSubstitute(ClearOptions.All);
        AdminService.ClearSubstitute(ClearOptions.All);
        RoleManagementService.ClearSubstitute(ClearOptions.All);
        JobManagementService.ClearSubstitute(ClearOptions.All);
        EmailService.ClearSubstitute(ClearOptions.All);
        HybridCache.ClearSubstitute(ClearOptions.All);
        CaptchaService.ClearSubstitute(ClearOptions.All);
        AuditService.ClearSubstitute(ClearOptions.All);
        TwoFactorService.ClearSubstitute(ClearOptions.All);
        ExternalAuthService.ClearSubstitute(ClearOptions.All);
        ProviderConfigService.ClearSubstitute(ClearOptions.All);
    }
}
