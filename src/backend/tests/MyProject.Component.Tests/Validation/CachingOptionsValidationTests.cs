using System.ComponentModel.DataAnnotations;
using MyProject.Infrastructure.Caching.Options;

namespace MyProject.Component.Tests.Validation;

public class CachingOptionsValidationTests
{
    private static List<ValidationResult> Validate(object instance)
    {
        var context = new ValidationContext(instance);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(instance, context, results, validateAllProperties: true);
        return results;
    }

    #region CachingOptions.DefaultExpiration

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(1440)]
    public void DefaultExpiration_Positive_NoErrors(int minutes)
    {
        var options = new CachingOptions { DefaultExpiration = TimeSpan.FromMinutes(minutes) };

        var results = Validate(options);

        Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(CachingOptions.DefaultExpiration)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void DefaultExpiration_ZeroOrNegative_ReturnsError(int seconds)
    {
        var options = new CachingOptions { DefaultExpiration = TimeSpan.FromSeconds(seconds) };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CachingOptions.DefaultExpiration)));
    }

    #endregion
}
