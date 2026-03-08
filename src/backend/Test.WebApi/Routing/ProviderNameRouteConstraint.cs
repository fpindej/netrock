using System.Text.RegularExpressions;

namespace Test.WebApi.Routing;

/// <summary>
/// Route constraint that validates an OAuth provider name: letters only, max 32 characters.
/// </summary>
public partial class ProviderNameRouteConstraint : IRouteConstraint
{
    /// <inheritdoc />
    public bool Match(HttpContext? httpContext, IRouter? route, string routeKey,
        RouteValueDictionary values, RouteDirection routeDirection)
    {
        if (!values.TryGetValue(routeKey, out var value) || value is not string providerName)
        {
            return false;
        }

        return providerName.Length <= 32 && Pattern().IsMatch(providerName);
    }

    [GeneratedRegex(@"^[A-Za-z]+$")]
    private static partial Regex Pattern();
}
