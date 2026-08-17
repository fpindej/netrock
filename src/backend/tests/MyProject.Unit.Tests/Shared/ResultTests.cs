using MyProject.Shared;

namespace MyProject.Unit.Tests.Shared;

public class ResultTests
{
    private static readonly Error TestError = new("test_error", "something went wrong");

    [Fact]
    public void Success_ShouldSetIsSuccessTrue()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
    }

    [Fact]
    public void Success_ShouldHaveNullError()
    {
        var result = Result.Success();

        Assert.Null(result.Error);
    }

    [Fact]
    public void Success_ShouldHaveNullErrorType()
    {
        var result = Result.Success();

        Assert.Null(result.ErrorType);
    }

    [Fact]
    public void Failure_WithError_ShouldSetIsSuccessFalse()
    {
        var result = Result.Failure(TestError);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Failure_WithError_ShouldPreserveError()
    {
        var result = Result.Failure(TestError);

        Assert.NotNull(result.Error);
        Assert.Same(TestError, result.Error);
        Assert.Equal("test_error", result.Error.Code);
        Assert.Equal("something went wrong", result.Error.Message);
    }

    [Fact]
    public void Failure_WithError_ShouldDefaultToValidationErrorType()
    {
        var result = Result.Failure(TestError);

        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Theory]
    [InlineData(ErrorType.Validation)]
    [InlineData(ErrorType.Unauthorized)]
    [InlineData(ErrorType.NotFound)]
    public void Failure_WithErrorAndErrorType_ShouldPreserveErrorType(ErrorType errorType)
    {
        var result = Result.Failure(TestError, errorType);

        Assert.Equal(errorType, result.ErrorType);
    }

    [Fact]
    public void Failure_WithErrorAndErrorType_ShouldPreserveError()
    {
        var error = new Error("not_found", "not found");

        var result = Result.Failure(error, ErrorType.NotFound);

        Assert.Same(error, result.Error);
        Assert.False(result.IsSuccess);
    }
}
