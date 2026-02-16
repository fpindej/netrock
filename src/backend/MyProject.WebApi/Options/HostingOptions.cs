namespace MyProject.WebApi.Options;

/// <summary>
/// Root hosting configuration. Controls reverse proxy trust, HTTPS enforcement,
/// and other deployment-specific settings.
/// Maps to the "Hosting" section in appsettings.json.
/// </summary>
public sealed class HostingOptions
{
    public const string SectionName = "Hosting";

    /// <summary>
    /// Whether to force the request scheme to HTTPS.
    /// Enable when behind a TLS-terminating reverse proxy (nginx, ALB, Cloudflare)
    /// that connects to the app over plain HTTP internally.
    /// </summary>
    public bool ForceHttps { get; init; } = true;

    /// <summary>
    /// Trusted reverse proxy configuration for <c>X-Forwarded-For</c> / <c>X-Forwarded-Proto</c>.
    /// </summary>
    public ReverseProxyOptions ReverseProxy { get; init; } = new();

    /// <summary>
    /// Trusted reverse proxy networks and addresses.
    /// Without any entries, only loopback proxies are trusted (ASP.NET Core default).
    /// </summary>
    public sealed class ReverseProxyOptions
    {
        /// <summary>
        /// CIDR blocks of trusted proxy networks (e.g., "172.16.0.0/12" for Docker bridge).
        /// </summary>
        public string[] TrustedNetworks { get; init; } = [];

        /// <summary>
        /// Individual trusted proxy IP addresses (e.g., "10.0.0.5").
        /// </summary>
        public string[] TrustedProxies { get; init; } = [];
    }
}
