using System.ComponentModel.DataAnnotations;
using MyProject.Infrastructure.Features.Authentication.Options;

namespace MyProject.Component.Tests.Validation;

public class AuthenticationOptionsValidationTests
{
    private static List<ValidationResult> Validate(object instance)
    {
        var context = new ValidationContext(instance);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(instance, context, results, validateAllProperties: true);
        return results;
    }

    #region JwtOptions.AccessTokenLifetime

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(120)]
    public void JwtOptions_AccessTokenLifetime_ValidMinutes_NoErrors(int minutes)
    {
        var options = new AuthenticationOptions.JwtOptions
        {
            Key = "ThisIsATestSigningKeyWithAtLeast32Chars!",
            Issuer = "test",
            Audience = "test",
            AccessTokenLifetime = TimeSpan.FromMinutes(minutes)
        };

        var results = Validate(options);

        Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(AuthenticationOptions.JwtOptions.AccessTokenLifetime)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(121)]
    [InlineData(1440)]
    public void JwtOptions_AccessTokenLifetime_OutOfRange_ReturnsError(int minutes)
    {
        var options = new AuthenticationOptions.JwtOptions
        {
            Key = "ThisIsATestSigningKeyWithAtLeast32Chars!",
            Issuer = "test",
            Audience = "test",
            AccessTokenLifetime = TimeSpan.FromMinutes(minutes)
        };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AuthenticationOptions.JwtOptions.AccessTokenLifetime)));
    }

    #endregion

    #region RefreshTokenOptions.PersistentLifetime

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(365)]
    public void RefreshTokenOptions_PersistentLifetime_ValidDays_NoErrors(int days)
    {
        var options = new AuthenticationOptions.JwtOptions.RefreshTokenOptions
        {
            PersistentLifetime = TimeSpan.FromDays(days),
            SessionLifetime = TimeSpan.FromHours(12)
        };

        var results = Validate(options);

        Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(AuthenticationOptions.JwtOptions.RefreshTokenOptions.PersistentLifetime)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(366)]
    public void RefreshTokenOptions_PersistentLifetime_OutOfRange_ReturnsError(int days)
    {
        var options = new AuthenticationOptions.JwtOptions.RefreshTokenOptions
        {
            PersistentLifetime = TimeSpan.FromDays(days),
            SessionLifetime = TimeSpan.FromHours(1)
        };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AuthenticationOptions.JwtOptions.RefreshTokenOptions.PersistentLifetime)));
    }

    #endregion

    #region RefreshTokenOptions.SessionLifetime

    [Theory]
    [InlineData(10)]
    [InlineData(60)]
    [InlineData(43200)] // 30 days in minutes
    public void RefreshTokenOptions_SessionLifetime_ValidMinutes_NoErrors(int minutes)
    {
        var options = new AuthenticationOptions.JwtOptions.RefreshTokenOptions
        {
            PersistentLifetime = TimeSpan.FromDays(365),
            SessionLifetime = TimeSpan.FromMinutes(minutes)
        };

        var results = Validate(options);

        Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(AuthenticationOptions.JwtOptions.RefreshTokenOptions.SessionLifetime)));
    }

    [Theory]
    [InlineData(9)]
    [InlineData(43201)] // 30 days + 1 minute
    public void RefreshTokenOptions_SessionLifetime_OutOfRange_ReturnsError(int minutes)
    {
        var options = new AuthenticationOptions.JwtOptions.RefreshTokenOptions
        {
            PersistentLifetime = TimeSpan.FromDays(365),
            SessionLifetime = TimeSpan.FromMinutes(minutes)
        };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AuthenticationOptions.JwtOptions.RefreshTokenOptions.SessionLifetime)));
    }

    [Fact]
    public void RefreshTokenOptions_SessionLifetimeExceedsPersistent_ReturnsError()
    {
        var options = new AuthenticationOptions.JwtOptions.RefreshTokenOptions
        {
            PersistentLifetime = TimeSpan.FromDays(1),
            SessionLifetime = TimeSpan.FromDays(2)
        };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AuthenticationOptions.JwtOptions.RefreshTokenOptions.SessionLifetime)));
    }

    [Fact]
    public void RefreshTokenOptions_SessionLifetimeEqualsPersistent_NoErrors()
    {
        var options = new AuthenticationOptions.JwtOptions.RefreshTokenOptions
        {
            PersistentLifetime = TimeSpan.FromDays(7),
            SessionLifetime = TimeSpan.FromDays(7)
        };

        var results = Validate(options);

        Assert.DoesNotContain(results, r =>
            r.MemberNames.Contains(nameof(AuthenticationOptions.JwtOptions.RefreshTokenOptions.SessionLifetime))
            && r.ErrorMessage!.Contains("must not exceed"));
    }

    #endregion

    #region EmailTokenOptions.Lifetime

    [Theory]
    [InlineData(1)]
    [InlineData(24)]
    [InlineData(168)] // 7 days
    public void EmailTokenOptions_Lifetime_ValidHours_NoErrors(int hours)
    {
        var options = new AuthenticationOptions.EmailTokenOptions
        {
            Lifetime = TimeSpan.FromHours(hours)
        };

        var results = Validate(options);

        Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(AuthenticationOptions.EmailTokenOptions.Lifetime)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(169)] // 7 days + 1 hour
    public void EmailTokenOptions_Lifetime_OutOfRange_ReturnsError(int hours)
    {
        var options = new AuthenticationOptions.EmailTokenOptions
        {
            Lifetime = TimeSpan.FromHours(hours)
        };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AuthenticationOptions.EmailTokenOptions.Lifetime)));
    }

    #endregion
}
