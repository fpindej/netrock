using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyProject.Application.Features.Email;
using MyProject.Infrastructure.Features.Email.Extensions;
using MyProject.Infrastructure.Features.Email.Jobs;
using MyProject.Infrastructure.Features.Email.Services;

namespace MyProject.Component.Tests.Extensions;

public class EmailServiceRegistrationTests
{
    private static ServiceCollection Register(bool emailEnabled, bool? jobSchedulingEnabled)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Email:Enabled"] = emailEnabled.ToString(),
            ["Email:FrontendBaseUrl"] = "https://test.example.com",
            ["Email:Smtp:Host"] = "localhost"
        };

        if (jobSchedulingEnabled is not null)
        {
            settings["JobScheduling:Enabled"] = jobSchedulingEnabled.Value.ToString();
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection();
        services.AddEmailServices(configuration);
        return services;
    }

    private static Type? ImplementationOf<TService>(ServiceCollection services) =>
        services.Single(d => d.ServiceType == typeof(TService)).ImplementationType;

    [Fact]
    public void AddEmailServices_EmailAndJobsEnabled_RegistersBackgroundEmailService()
    {
        var services = Register(emailEnabled: true, jobSchedulingEnabled: true);

        Assert.Equal(typeof(BackgroundEmailService), ImplementationOf<IEmailService>(services));
        Assert.Contains(services, d => d.ServiceType == typeof(EmailDeliveryJob));
        Assert.Contains(services, d => d.ServiceType == typeof(SmtpEmailService));
    }

    [Fact]
    public void AddEmailServices_EmailEnabledJobsDefault_RegistersBackgroundEmailService()
    {
        // JobScheduling:Enabled defaults to true when the section is absent.
        var services = Register(emailEnabled: true, jobSchedulingEnabled: null);

        Assert.Equal(typeof(BackgroundEmailService), ImplementationOf<IEmailService>(services));
    }

    [Fact]
    public void AddEmailServices_EmailEnabledJobsDisabled_RegistersSmtpEmailService()
    {
        var services = Register(emailEnabled: true, jobSchedulingEnabled: false);

        Assert.Equal(typeof(SmtpEmailService), ImplementationOf<IEmailService>(services));
        Assert.DoesNotContain(services, d => d.ServiceType == typeof(EmailDeliveryJob));
    }

    [Fact]
    public void AddEmailServices_EmailDisabled_RegistersNoOpEmailService()
    {
        var services = Register(emailEnabled: false, jobSchedulingEnabled: true);

        Assert.Equal(typeof(NoOpEmailService), ImplementationOf<IEmailService>(services));
        Assert.DoesNotContain(services, d => d.ServiceType == typeof(EmailDeliveryJob));
        Assert.DoesNotContain(services, d => d.ServiceType == typeof(SmtpEmailService));
    }

    [Fact]
    public void AddEmailServices_AlwaysRegistersTemplatePipeline()
    {
        var services = Register(emailEnabled: false, jobSchedulingEnabled: false);

        Assert.Equal(typeof(FluidEmailTemplateRenderer), ImplementationOf<IEmailTemplateRenderer>(services));
        Assert.Equal(typeof(TemplatedEmailSender), ImplementationOf<ITemplatedEmailSender>(services));
    }
}
