using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using MyProject.WebApi.Options;

namespace MyProject.WebApi.Extensions;

/// <summary>
/// Extension methods for configuring the <c>ForwardedHeaders</c> middleware
/// with trusted proxy networks and addresses read from configuration.
/// </summary>
internal static class ForwardedHeadersExtensions
{
    /// <summary>
    /// Registers <see cref="ReverseProxyOptions"/> from the configuration section
    /// and validates them at startup.
    /// </summary>
    public static IServiceCollection AddForwardedHeaders(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ReverseProxyOptions>()
            .BindConfiguration(ReverseProxyOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }

    /// <summary>
    /// Applies the <c>ForwardedHeaders</c> middleware, trusting proxy networks and addresses
    /// from the <c>ForwardedHeaders</c> configuration section. Without trusted entries,
    /// only loopback proxies are accepted (ASP.NET Core default).
    /// </summary>
    /// <remarks>
    /// Configuration example (environment variables):
    /// <code>
    /// ForwardedHeaders__TrustedNetworks__0=172.16.0.0/12
    /// ForwardedHeaders__TrustedProxies__0=10.0.0.5
    /// </code>
    /// Configuration example (appsettings.json):
    /// <code>
    /// {
    ///   "ForwardedHeaders": {
    ///     "TrustedNetworks": ["172.16.0.0/12"],
    ///     "TrustedProxies": ["10.0.0.5"]
    ///   }
    /// }
    /// </code>
    /// </remarks>
    public static IApplicationBuilder UseConfiguredForwardedHeaders(this IApplicationBuilder app)
    {
        var proxyOptions = app.ApplicationServices
            .GetRequiredService<IOptions<ReverseProxyOptions>>().Value;

        var options = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        };

        foreach (var cidr in proxyOptions.TrustedNetworks)
        {
            options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(cidr));
        }

        foreach (var proxy in proxyOptions.TrustedProxies)
        {
            options.KnownProxies.Add(IPAddress.Parse(proxy));
        }

        app.UseForwardedHeaders(options);

        return app;
    }
}
