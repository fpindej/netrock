namespace MyProject.Application.Cookies;

public interface ICookieService
{
    void SetCookie(string key, string value, DateTimeOffset? expires = null);
    void SetSecureCookie(string key, string value, DateTimeOffset? expires = null);
    void DeleteCookie(string key);
    string? GetCookie(string key);
}
