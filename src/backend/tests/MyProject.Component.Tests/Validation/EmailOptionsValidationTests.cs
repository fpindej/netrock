using System.ComponentModel.DataAnnotations;
using MyProject.Infrastructure.Features.Email.Options;

namespace MyProject.Component.Tests.Validation;

public class EmailOptionsValidationTests
{
    private static List<ValidationResult> Validate(object instance)
    {
        var context = new ValidationContext(instance);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(instance, context, results, validateAllProperties: true);
        return results;
    }

    #region FromAddress

    [Theory]
    [InlineData("noreply@example.com")]
    [InlineData("support@my-domain.org")]
    public void FromAddress_ValidEmail_NoErrors(string email)
    {
        var options = new EmailOptions
        {
            FromAddress = email,
            FromName = "Test",
            FrontendBaseUrl = "https://example.com"
        };

        var results = Validate(options);

        Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(EmailOptions.FromAddress)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("missing@")]
    public void FromAddress_InvalidOrEmpty_ReturnsError(string email)
    {
        var options = new EmailOptions
        {
            FromAddress = email,
            FromName = "Test",
            FrontendBaseUrl = "https://example.com"
        };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(EmailOptions.FromAddress)));
    }

    #endregion

    #region FromName

    [Fact]
    public void FromName_Valid_NoErrors()
    {
        var options = new EmailOptions
        {
            FromAddress = "noreply@example.com",
            FromName = "MyApp",
            FrontendBaseUrl = "https://example.com"
        };

        var results = Validate(options);

        Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(EmailOptions.FromName)));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void FromName_EmptyOrNull_ReturnsError(string? name)
    {
        var options = new EmailOptions
        {
            FromAddress = "noreply@example.com",
            FromName = name!,
            FrontendBaseUrl = "https://example.com"
        };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(EmailOptions.FromName)));
    }

    #endregion

    #region FrontendBaseUrl

    [Fact]
    public void FrontendBaseUrl_Valid_NoErrors()
    {
        var options = new EmailOptions
        {
            FromAddress = "noreply@example.com",
            FromName = "Test",
            FrontendBaseUrl = "https://example.com"
        };

        var results = Validate(options);

        Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(EmailOptions.FrontendBaseUrl)));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void FrontendBaseUrl_EmptyOrNull_ReturnsError(string? url)
    {
        var options = new EmailOptions
        {
            FromAddress = "noreply@example.com",
            FromName = "Test",
            FrontendBaseUrl = url!
        };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(EmailOptions.FrontendBaseUrl)));
    }

    #endregion

    #region Defaults

    [Fact]
    public void DefaultValues_FromAddressAndFromNamePreset_OnlyFrontendBaseUrlRequired()
    {
        // EmailOptions has defaults for FromAddress and FromName but FrontendBaseUrl is empty string
        var options = new EmailOptions();

        var results = Validate(options);

        // FrontendBaseUrl defaults to empty string which fails [Required]
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(EmailOptions.FrontendBaseUrl)));
        Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(EmailOptions.FromAddress)));
        Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(EmailOptions.FromName)));
    }

    #endregion
}
