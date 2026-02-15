using MyProject.Shared;

namespace MyProject.Unit.Tests.Shared;

public class ResultTests
{
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
    public void Failure_WithMessage_ShouldSetIsSuccessFalse()
    {
        var result = Result.Failure("something went wrong");

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Failure_WithMessage_ShouldPreserveError()
    {
        var result = Result.Failure("something went wrong");

        Assert.Equal("something went wrong", result.Error);
    }

    [Fact]
    public void Failure_WithMessage_ShouldDefaultToValidationErrorType()
    {
        var result = Result.Failure("something went wrong");

        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Theory]
    [InlineData(ErrorType.Validation)]
    [InlineData(ErrorType.Unauthorized)]
    [InlineData(ErrorType.NotFound)]
    public void Failure_WithMessageAndErrorType_ShouldPreserveErrorType(ErrorType errorType)
    {
        var result = Result.Failure("error", errorType);

        Assert.Equal(errorType, result.ErrorType);
    }

    [Fact]
    public void Failure_WithMessageAndErrorType_ShouldPreserveError()
    {
        var result = Result.Failure("not found", ErrorType.NotFound);

        Assert.Equal("not found", result.Error);
        Assert.False(result.IsSuccess);
    }
}
