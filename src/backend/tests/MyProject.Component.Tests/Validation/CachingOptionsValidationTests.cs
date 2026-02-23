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

    #region CircuitBreakerOptions.FailureThreshold

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(100)]
    public void CircuitBreakerOptions_FailureThreshold_ValidRange_NoErrors(int threshold)
    {
        var options = new CachingOptions.CircuitBreakerOptions
        {
            FailureThreshold = threshold
        };

        var results = Validate(options);

        Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(CachingOptions.CircuitBreakerOptions.FailureThreshold)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    public void CircuitBreakerOptions_FailureThreshold_OutOfRange_ReturnsError(int threshold)
    {
        var options = new CachingOptions.CircuitBreakerOptions
        {
            FailureThreshold = threshold
        };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CachingOptions.CircuitBreakerOptions.FailureThreshold)));
    }

    #endregion

    #region CircuitBreakerOptions.BreakDuration

    [Fact]
    public void CircuitBreakerOptions_BreakDuration_Positive_NoErrors()
    {
        var options = new CachingOptions.CircuitBreakerOptions
        {
            BreakDuration = TimeSpan.FromSeconds(10),
            SamplingDuration = TimeSpan.FromSeconds(30)
        };

        var results = Validate(options);

        Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(CachingOptions.CircuitBreakerOptions.BreakDuration)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CircuitBreakerOptions_BreakDuration_ZeroOrNegative_ReturnsError(int seconds)
    {
        var options = new CachingOptions.CircuitBreakerOptions
        {
            BreakDuration = TimeSpan.FromSeconds(seconds)
        };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CachingOptions.CircuitBreakerOptions.BreakDuration)));
    }

    #endregion

    #region CircuitBreakerOptions.SamplingDuration

    [Fact]
    public void CircuitBreakerOptions_SamplingDuration_Positive_NoErrors()
    {
        var options = new CachingOptions.CircuitBreakerOptions
        {
            BreakDuration = TimeSpan.FromSeconds(10),
            SamplingDuration = TimeSpan.FromSeconds(30)
        };

        var results = Validate(options);

        Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(CachingOptions.CircuitBreakerOptions.SamplingDuration)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CircuitBreakerOptions_SamplingDuration_ZeroOrNegative_ReturnsError(int seconds)
    {
        var options = new CachingOptions.CircuitBreakerOptions
        {
            SamplingDuration = TimeSpan.FromSeconds(seconds)
        };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CachingOptions.CircuitBreakerOptions.SamplingDuration)));
    }

    #endregion

    #region CircuitBreakerOptions — Cross-property

    [Fact]
    public void CircuitBreakerOptions_SamplingDurationLessThanBreakDuration_ReturnsError()
    {
        var options = new CachingOptions.CircuitBreakerOptions
        {
            BreakDuration = TimeSpan.FromSeconds(60),
            SamplingDuration = TimeSpan.FromSeconds(10)
        };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CachingOptions.CircuitBreakerOptions.SamplingDuration)));
    }

    [Fact]
    public void CircuitBreakerOptions_SamplingDurationEqualsBreakDuration_NoErrors()
    {
        var options = new CachingOptions.CircuitBreakerOptions
        {
            BreakDuration = TimeSpan.FromSeconds(30),
            SamplingDuration = TimeSpan.FromSeconds(30)
        };

        var results = Validate(options);

        Assert.DoesNotContain(results, r =>
            r.MemberNames.Contains(nameof(CachingOptions.CircuitBreakerOptions.SamplingDuration))
            && r.ErrorMessage!.Contains("greater than or equal"));
    }

    [Fact]
    public void CircuitBreakerOptions_SamplingDurationExceedsBreakDuration_NoErrors()
    {
        var options = new CachingOptions.CircuitBreakerOptions
        {
            BreakDuration = TimeSpan.FromSeconds(10),
            SamplingDuration = TimeSpan.FromSeconds(60)
        };

        var results = Validate(options);

        Assert.DoesNotContain(results, r =>
            r.MemberNames.Contains(nameof(CachingOptions.CircuitBreakerOptions.SamplingDuration))
            && r.ErrorMessage!.Contains("greater than or equal"));
    }

    #endregion
}
