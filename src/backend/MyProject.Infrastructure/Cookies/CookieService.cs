using Microsoft.AspNetCore.Http;
using MyProject.Application.Cookies;

namespace MyProject.Infrastructure.Cookies;

public class CookieService(IHttpContextAccessor httpContextAccessor) : ICookieService
{
    public void SetCookie(string key, string value, DateTimeOffset? expires = null)
    {
        var options = new CookieOptions
        {
            HttpOnly = false,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = expires
        };

        httpContextAccessor.HttpContext?.Response.Cookies.Append(key, value, options);
    }

    public void SetSecureCookie(string key, string value, DateTimeOffset? expires = null)
    {
        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = expires
        };

        httpContextAccessor.HttpContext?.Response.Cookies.Append(key, value, options);
    }

    public void DeleteCookie(string key)
    {
        httpContextAccessor.HttpContext?.Response.Cookies.Delete(key, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None
        });
    }

    public string? GetCookie(string key)
    {
        return httpContextAccessor.HttpContext?.Request.Cookies[key];
    }
}
