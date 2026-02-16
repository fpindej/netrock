using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using MyProject.WebApi.Options;

namespace MyProject.WebApi.Extensions;

/// <summary>
/// Extension methods for configuring hosting infrastructure: reverse proxy trust,
/// forwarded headers, and HTTPS enforcement.
/// </summary>
internal static class HostingExtensions
{
    /// <summary>
    /// Registers <see cref="HostingOptions"/> from the "Hosting" configuration section
    /// and validates them at startup.
    /// </summary>
    public static IServiceCollection AddHostingOptions(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<HostingOptions>()
            .BindConfiguration(HostingOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }

    /// <summary>
    /// Applies forwarded headers middleware (trusting networks/proxies from config)
    /// and optionally forces the HTTPS scheme when <see cref="HostingOptions.ForceHttps"/> is enabled.
    /// </summary>
    public static IApplicationBuilder UseHostingMiddleware(this IApplicationBuilder app)
    {
        var hostingOptions = app.ApplicationServices
            .GetRequiredService<IOptions<HostingOptions>>().Value;

        // Forwarded headers — trust configured reverse proxy networks/addresses
        var forwardedHeadersOptions = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        };

        foreach (var cidr in hostingOptions.ReverseProxy.TrustedNetworks)
        {
            forwardedHeadersOptions.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(cidr));
        }

        foreach (var proxy in hostingOptions.ReverseProxy.TrustedProxies)
        {
            forwardedHeadersOptions.KnownProxies.Add(IPAddress.Parse(proxy));
        }

        app.UseForwardedHeaders(forwardedHeadersOptions);

        // HTTPS scheme override — for TLS-terminating proxies that connect over HTTP internally
        if (hostingOptions.ForceHttps)
        {
            app.Use(async (context, next) =>
            {
                context.Request.Scheme = "https";
                await next();
            });
        }

        return app;
    }
}
