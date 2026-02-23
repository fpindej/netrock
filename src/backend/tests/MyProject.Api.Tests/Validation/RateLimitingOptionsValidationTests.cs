using System.ComponentModel.DataAnnotations;
using MyProject.WebApi.Options;

namespace MyProject.Api.Tests.Validation;

public class RateLimitingOptionsValidationTests
{
    private static List<ValidationResult> Validate(object instance)
    {
        var context = new ValidationContext(instance);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(instance, context, results, validateAllProperties: true);
        return results;
    }

    #region PermitLimit

    [Theory]
    [InlineData(1)]
    [InlineData(120)]
    [InlineData(10000)]
    public void PermitLimit_ValidRange_NoErrors(int limit)
    {
        var options = new RateLimitingOptions.GlobalLimitOptions
        {
            PermitLimit = limit,
            Window = TimeSpan.FromMinutes(1)
        };

        var results = Validate(options);

        Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(RateLimitingOptions.GlobalLimitOptions.PermitLimit)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10001)]
    public void PermitLimit_OutOfRange_ReturnsError(int limit)
    {
        var options = new RateLimitingOptions.GlobalLimitOptions
        {
            PermitLimit = limit,
            Window = TimeSpan.FromMinutes(1)
        };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(RateLimitingOptions.GlobalLimitOptions.PermitLimit)));
    }

    #endregion

    #region Window

    [Theory]
    [InlineData(1)]
    [InlineData(60)]
    [InlineData(3600)]
    public void Window_Positive_NoErrors(int seconds)
    {
        var options = new RateLimitingOptions.GlobalLimitOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromSeconds(seconds)
        };

        var results = Validate(options);

        Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(RateLimitingOptions.GlobalLimitOptions.Window)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Window_ZeroOrNegative_ReturnsError(int seconds)
    {
        var options = new RateLimitingOptions.GlobalLimitOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromSeconds(seconds)
        };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(RateLimitingOptions.GlobalLimitOptions.Window)));
    }

    #endregion

    #region QueueLimit

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(500)]
    [InlineData(1000)]
    public void QueueLimit_ValidRange_NoErrors(int limit)
    {
        var options = new RateLimitingOptions.GlobalLimitOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = limit
        };

        var results = Validate(options);

        Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(RateLimitingOptions.GlobalLimitOptions.QueueLimit)));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1001)]
    public void QueueLimit_OutOfRange_ReturnsError(int limit)
    {
        var options = new RateLimitingOptions.GlobalLimitOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = limit
        };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(RateLimitingOptions.GlobalLimitOptions.QueueLimit)));
    }

    #endregion

    #region Defaults

    [Fact]
    public void GlobalLimitOptions_DefaultValues_Valid()
    {
        var options = new RateLimitingOptions.GlobalLimitOptions();

        var results = Validate(options);

        Assert.Empty(results);
    }

    [Fact]
    public void RegistrationLimitOptions_DefaultValues_Valid()
    {
        var options = new RateLimitingOptions.RegistrationLimitOptions();

        var results = Validate(options);

        Assert.Empty(results);
    }

    [Fact]
    public void AuthLimitOptions_DefaultValues_Valid()
    {
        var options = new RateLimitingOptions.AuthLimitOptions();

        var results = Validate(options);

        Assert.Empty(results);
    }

    [Fact]
    public void SensitiveLimitOptions_DefaultValues_Valid()
    {
        var options = new RateLimitingOptions.SensitiveLimitOptions();

        var results = Validate(options);

        Assert.Empty(results);
    }

    [Fact]
    public void AdminMutationsLimitOptions_DefaultValues_Valid()
    {
        var options = new RateLimitingOptions.AdminMutationsLimitOptions();

        var results = Validate(options);

        Assert.Empty(results);
    }

    #endregion
}
