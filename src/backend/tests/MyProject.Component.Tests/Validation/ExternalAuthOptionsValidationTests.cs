using System.ComponentModel.DataAnnotations;
using MyProject.Infrastructure.Features.Authentication.Options;

namespace MyProject.Component.Tests.Validation;

public class ExternalAuthOptionsValidationTests
{
    private static List<ValidationResult> Validate(ExternalAuthOptions options)
    {
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(options, context, results, validateAllProperties: true);

        results.AddRange(options.Validate(context));

        return results;
    }

    #region AllowedRedirectUris

    [Fact]
    public void AllowedRedirectUris_ValidHttpsUri_NoErrors()
    {
        var options = new ExternalAuthOptions
        {
            AllowedRedirectUris = ["https://app.example.com/oauth/callback"]
        };

        var results = Validate(options);

        Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(ExternalAuthOptions.AllowedRedirectUris)));
    }

    [Fact]
    public void AllowedRedirectUris_ValidHttpUri_NoErrors()
    {
        var options = new ExternalAuthOptions
        {
            AllowedRedirectUris = ["http://localhost:5173/oauth/callback"]
        };

        var results = Validate(options);

        Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(ExternalAuthOptions.AllowedRedirectUris)));
    }

    [Fact]
    public void AllowedRedirectUris_CustomSchemeForMobileApp_NoErrors()
    {
        var options = new ExternalAuthOptions
        {
            AllowedRedirectUris = ["myapp://oauth/callback"]
        };

        var results = Validate(options);

        Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(ExternalAuthOptions.AllowedRedirectUris)));
    }

    [Fact]
    public void AllowedRedirectUris_InvalidUri_ReturnsError()
    {
        var options = new ExternalAuthOptions
        {
            AllowedRedirectUris = ["not-a-valid-uri"]
        };

        var results = Validate(options);

        Assert.Contains(results, r =>
            r.MemberNames.Contains(nameof(ExternalAuthOptions.AllowedRedirectUris))
            && r.ErrorMessage!.Contains("invalid URI"));
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,<h1>pwned</h1>")]
    [InlineData("file:///etc/passwd")]
    [InlineData("blob:https://evil.com/abc")]
    [InlineData("vbscript:MsgBox")]
    public void AllowedRedirectUris_DangerousScheme_ReturnsError(string uri)
    {
        var options = new ExternalAuthOptions
        {
            AllowedRedirectUris = [uri]
        };

        var results = Validate(options);

        Assert.Contains(results, r =>
            r.MemberNames.Contains(nameof(ExternalAuthOptions.AllowedRedirectUris))
            && r.ErrorMessage!.Contains("dangerous scheme"));
    }

    #endregion

    #region StateLifetime

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(30)]
    public void StateLifetime_ValidRange_NoErrors(int minutes)
    {
        var options = new ExternalAuthOptions
        {
            AllowedRedirectUris = ["https://app.example.com/callback"],
            StateLifetime = TimeSpan.FromMinutes(minutes)
        };

        var results = Validate(options);

        Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(ExternalAuthOptions.StateLifetime)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(31)]
    [InlineData(60)]
    public void StateLifetime_OutOfRange_ReturnsError(int minutes)
    {
        var options = new ExternalAuthOptions
        {
            AllowedRedirectUris = ["https://app.example.com/callback"],
            StateLifetime = TimeSpan.FromMinutes(minutes)
        };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(ExternalAuthOptions.StateLifetime)));
    }

    #endregion

}
