using System.ComponentModel.DataAnnotations;
using MyProject.WebApi.Options;

namespace MyProject.Api.Tests.Validation;

public class CorsOptionsValidationTests
{
    private static List<ValidationResult> Validate(object instance)
    {
        var context = new ValidationContext(instance);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(instance, context, results, validateAllProperties: true);
        return results;
    }

    #region AllowedOrigins — Conditional

    [Fact]
    public void AllowAllOriginsFalse_EmptyOrigins_ReturnsError()
    {
        var options = new CorsOptions
        {
            AllowAllOrigins = false,
            AllowedOrigins = []
        };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CorsOptions.AllowedOrigins)));
    }

    [Fact]
    public void AllowAllOriginsFalse_WithOrigins_NoErrors()
    {
        var options = new CorsOptions
        {
            AllowAllOrigins = false,
            AllowedOrigins = ["https://example.com"]
        };

        var results = Validate(options);

        Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(CorsOptions.AllowedOrigins)));
    }

    [Fact]
    public void AllowAllOriginsTrue_EmptyOrigins_NoErrors()
    {
        var options = new CorsOptions
        {
            AllowAllOrigins = true,
            AllowedOrigins = []
        };

        var results = Validate(options);

        Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(CorsOptions.AllowedOrigins)));
    }

    #endregion

    #region PolicyName

    [Fact]
    public void PolicyName_Valid_NoErrors()
    {
        var options = new CorsOptions
        {
            AllowAllOrigins = true,
            PolicyName = "MyCorsPolicy"
        };

        var results = Validate(options);

        Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(CorsOptions.PolicyName)));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void PolicyName_EmptyOrNull_ReturnsError(string? name)
    {
        var options = new CorsOptions
        {
            AllowAllOrigins = true,
            PolicyName = name!
        };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CorsOptions.PolicyName)));
    }

    #endregion

    #region Defaults

    [Fact]
    public void DefaultValues_AllowAllOriginsFalseAndNoOrigins_ReturnsError()
    {
        // Default: AllowAllOrigins = false, AllowedOrigins = [] — requires origins
        var options = new CorsOptions();

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CorsOptions.AllowedOrigins)));
    }

    #endregion
}
