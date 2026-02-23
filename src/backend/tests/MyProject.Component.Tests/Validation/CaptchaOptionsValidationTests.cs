using System.ComponentModel.DataAnnotations;
using MyProject.Infrastructure.Features.Captcha.Options;

namespace MyProject.Component.Tests.Validation;

public class CaptchaOptionsValidationTests
{
    private static List<ValidationResult> Validate(object instance)
    {
        var context = new ValidationContext(instance);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(instance, context, results, validateAllProperties: true);
        return results;
    }

    #region SecretKey

    [Fact]
    public void SecretKey_Valid_NoErrors()
    {
        var options = new CaptchaOptions { SecretKey = "0x4AAAAAABcEe..." };

        var results = Validate(options);

        Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(CaptchaOptions.SecretKey)));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void SecretKey_EmptyOrNull_ReturnsError(string? key)
    {
        var options = new CaptchaOptions { SecretKey = key! };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CaptchaOptions.SecretKey)));
    }

    #endregion

    #region Defaults

    [Fact]
    public void DefaultValues_SecretKeyEmpty_ReturnsError()
    {
        // SecretKey defaults to string.Empty which fails [Required]
        var options = new CaptchaOptions();

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CaptchaOptions.SecretKey)));
    }

    #endregion
}
