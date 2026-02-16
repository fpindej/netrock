namespace MyProject.WebApi.Options;

/// <summary>
/// Configuration for trusted reverse proxy networks and addresses.
/// Maps to the "ForwardedHeaders" section in appsettings.json.
/// Without any entries, only loopback proxies are trusted (ASP.NET Core default).
/// </summary>
public sealed class ReverseProxyOptions
{
    public const string SectionName = "ForwardedHeaders";

    /// <summary>
    /// CIDR blocks of trusted proxy networks (e.g., "172.16.0.0/12" for Docker bridge).
    /// </summary>
    public string[] TrustedNetworks { get; init; } = [];

    /// <summary>
    /// Individual trusted proxy IP addresses (e.g., "10.0.0.5").
    /// </summary>
    public string[] TrustedProxies { get; init; } = [];
}
