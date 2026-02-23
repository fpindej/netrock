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

    #region RedisOptions — Conditional Validation

    [Fact]
    public void RedisOptions_WhenDisabled_SkipsAllValidation()
    {
        var options = new CachingOptions.RedisOptions
        {
            Enabled = false,
            ConnectionString = string.Empty // would fail if enabled
        };

        var results = Validate(options);

        Assert.Empty(results);
    }

    [Fact]
    public void RedisOptions_WhenEnabled_ValidConfig_NoErrors()
    {
        var options = new CachingOptions.RedisOptions
        {
            Enabled = true,
            ConnectionString = "localhost:6379"
        };

        var results = Validate(options);

        Assert.Empty(results);
    }

    [Fact]
    public void RedisOptions_WhenEnabled_EmptyConnectionString_ReturnsError()
    {
        var options = new CachingOptions.RedisOptions
        {
            Enabled = true,
            ConnectionString = string.Empty
        };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CachingOptions.RedisOptions.ConnectionString)));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(16)]
    public void RedisOptions_WhenEnabled_DefaultDatabaseOutOfRange_ReturnsError(int database)
    {
        var options = new CachingOptions.RedisOptions
        {
            Enabled = true,
            ConnectionString = "localhost:6379",
            DefaultDatabase = database
        };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CachingOptions.RedisOptions.DefaultDatabase)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    [InlineData(15)]
    public void RedisOptions_WhenEnabled_DefaultDatabaseValid_NoErrors(int database)
    {
        var options = new CachingOptions.RedisOptions
        {
            Enabled = true,
            ConnectionString = "localhost:6379",
            DefaultDatabase = database
        };

        var results = Validate(options);

        Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(CachingOptions.RedisOptions.DefaultDatabase)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RedisOptions_WhenEnabled_ConnectTimeoutMsInvalid_ReturnsError(int timeout)
    {
        var options = new CachingOptions.RedisOptions
        {
            Enabled = true,
            ConnectionString = "localhost:6379",
            ConnectTimeoutMs = timeout
        };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CachingOptions.RedisOptions.ConnectTimeoutMs)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RedisOptions_WhenEnabled_SyncTimeoutMsInvalid_ReturnsError(int timeout)
    {
        var options = new CachingOptions.RedisOptions
        {
            Enabled = true,
            ConnectionString = "localhost:6379",
            SyncTimeoutMs = timeout
        };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CachingOptions.RedisOptions.SyncTimeoutMs)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RedisOptions_WhenEnabled_AsyncTimeoutMsInvalid_ReturnsError(int timeout)
    {
        var options = new CachingOptions.RedisOptions
        {
            Enabled = true,
            ConnectionString = "localhost:6379",
            AsyncTimeoutMs = timeout
        };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CachingOptions.RedisOptions.AsyncTimeoutMs)));
    }

    [Fact]
    public void RedisOptions_WhenEnabled_NegativeConnectRetry_ReturnsError()
    {
        var options = new CachingOptions.RedisOptions
        {
            Enabled = true,
            ConnectionString = "localhost:6379",
            ConnectRetry = -1
        };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CachingOptions.RedisOptions.ConnectRetry)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void RedisOptions_WhenEnabled_ConnectRetryValid_NoErrors(int retries)
    {
        var options = new CachingOptions.RedisOptions
        {
            Enabled = true,
            ConnectionString = "localhost:6379",
            ConnectRetry = retries
        };

        var results = Validate(options);

        Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(CachingOptions.RedisOptions.ConnectRetry)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RedisOptions_WhenEnabled_KeepAliveSecondsInvalid_ReturnsError(int seconds)
    {
        var options = new CachingOptions.RedisOptions
        {
            Enabled = true,
            ConnectionString = "localhost:6379",
            KeepAliveSeconds = seconds
        };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CachingOptions.RedisOptions.KeepAliveSeconds)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(60)]
    public void RedisOptions_WhenEnabled_KeepAliveSecondsValid_NoErrors(int seconds)
    {
        var options = new CachingOptions.RedisOptions
        {
            Enabled = true,
            ConnectionString = "localhost:6379",
            KeepAliveSeconds = seconds
        };

        var results = Validate(options);

        Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(CachingOptions.RedisOptions.KeepAliveSeconds)));
    }

    #endregion

    #region InMemoryOptions

    [Theory]
    [InlineData(1)]
    [InlineData(1024)]
    [InlineData(10000)]
    public void InMemoryOptions_SizeLimit_ValidPositive_NoErrors(int sizeLimit)
    {
        var options = new CachingOptions.InMemoryOptions { SizeLimit = sizeLimit };

        var results = Validate(options);

        Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(CachingOptions.InMemoryOptions.SizeLimit)));
    }

    [Fact]
    public void InMemoryOptions_SizeLimit_Null_ReturnsError()
    {
        var options = new CachingOptions.InMemoryOptions { SizeLimit = null };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CachingOptions.InMemoryOptions.SizeLimit)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void InMemoryOptions_SizeLimit_ZeroOrNegative_ReturnsError(int sizeLimit)
    {
        var options = new CachingOptions.InMemoryOptions { SizeLimit = sizeLimit };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CachingOptions.InMemoryOptions.SizeLimit)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void InMemoryOptions_ExpirationScanFrequency_ZeroOrNegative_ReturnsError(int seconds)
    {
        var options = new CachingOptions.InMemoryOptions
        {
            ExpirationScanFrequency = TimeSpan.FromSeconds(seconds)
        };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CachingOptions.InMemoryOptions.ExpirationScanFrequency)));
    }

    [Fact]
    public void InMemoryOptions_ExpirationScanFrequency_Positive_NoErrors()
    {
        var options = new CachingOptions.InMemoryOptions
        {
            ExpirationScanFrequency = TimeSpan.FromMinutes(1)
        };

        var results = Validate(options);

        Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(CachingOptions.InMemoryOptions.ExpirationScanFrequency)));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void InMemoryOptions_CompactionPercentage_OutOfRange_ReturnsError(double percentage)
    {
        var options = new CachingOptions.InMemoryOptions { CompactionPercentage = percentage };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CachingOptions.InMemoryOptions.CompactionPercentage)));
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(0.05)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void InMemoryOptions_CompactionPercentage_ValidRange_NoErrors(double percentage)
    {
        var options = new CachingOptions.InMemoryOptions { CompactionPercentage = percentage };

        var results = Validate(options);

        Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(CachingOptions.InMemoryOptions.CompactionPercentage)));
    }

    #endregion

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
