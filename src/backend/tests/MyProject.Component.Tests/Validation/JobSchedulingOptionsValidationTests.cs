using System.ComponentModel.DataAnnotations;
using MyProject.Infrastructure.Features.Jobs.Options;

namespace MyProject.Component.Tests.Validation;

public class JobSchedulingOptionsValidationTests
{
    private static List<ValidationResult> Validate(object instance)
    {
        var context = new ValidationContext(instance);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(instance, context, results, validateAllProperties: true);
        return results;
    }

    #region WorkerCount

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(100)]
    [InlineData(1000)]
    public void WorkerCount_ValidRange_NoErrors(int count)
    {
        var options = new JobSchedulingOptions { WorkerCount = count };

        var results = Validate(options);

        Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(JobSchedulingOptions.WorkerCount)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1001)]
    public void WorkerCount_OutOfRange_ReturnsError(int count)
    {
        var options = new JobSchedulingOptions { WorkerCount = count };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(JobSchedulingOptions.WorkerCount)));
    }

    #endregion

    #region Defaults

    [Fact]
    public void DefaultValues_Valid()
    {
        // WorkerCount defaults to Environment.ProcessorCount which should be in range
        var options = new JobSchedulingOptions();

        var results = Validate(options);

        Assert.Empty(results);
    }

    #endregion
}
