using MyProject.Shared;

namespace MyProject.Unit.Tests.Shared;

public class ResultGenericTests
{
    private static readonly Error TestError = new("test_error", "error");

    [Fact]
    public void Success_ShouldSetIsSuccessTrue()
    {
        var result = Result<int>.Success(42);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
    }

    [Fact]
    public void Success_ShouldReturnValue()
    {
        var result = Result<int>.Success(42);

        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Success_ShouldHaveNullError()
    {
        var result = Result<string>.Success("hello");

        Assert.Null(result.Error);
        Assert.Null(result.ErrorType);
    }

    [Fact]
    public void Failure_ShouldSetIsSuccessFalse()
    {
        var result = Result<int>.Failure(TestError);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Failure_ShouldPreserveError()
    {
        var error = new Error("something_broke", "something broke");

        var result = Result<int>.Failure(error);

        Assert.NotNull(result.Error);
        Assert.Same(error, result.Error);
        Assert.Equal("something_broke", result.Error.Code);
        Assert.Equal("something broke", result.Error.Message);
    }

    [Fact]
    public void Failure_WithError_ShouldDefaultToValidationErrorType()
    {
        var result = Result<int>.Failure(TestError);

        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Theory]
    [InlineData(ErrorType.Validation)]
    [InlineData(ErrorType.Unauthorized)]
    [InlineData(ErrorType.NotFound)]
    public void Failure_WithErrorAndErrorType_ShouldPreserveErrorType(ErrorType errorType)
    {
        var result = Result<string>.Failure(TestError, errorType);

        Assert.Equal(errorType, result.ErrorType);
    }

    [Fact]
    public void Value_OnFailure_ShouldThrowInvalidOperationException()
    {
        var result = Result<int>.Failure(TestError);

        var exception = Assert.Throws<InvalidOperationException>(() => result.Value);
        Assert.Equal("Cannot access Value on a failed result.", exception.Message);
    }

    [Fact]
    public void Success_WithReferenceType_ShouldReturnValue()
    {
        var list = new List<int> { 1, 2, 3 };
        var result = Result<List<int>>.Success(list);

        Assert.Same(list, result.Value);
    }

    [Fact]
    public void ResultGeneric_InheritsFromResult()
    {
        Result result = Result<int>.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
    }

    [Fact]
    public void ResultGeneric_Failure_InheritsFromResult()
    {
        Result result = Result<int>.Failure(TestError, ErrorType.NotFound);

        Assert.True(result.IsFailure);
        Assert.Equal(TestError, result.Error);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }
}
