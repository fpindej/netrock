using MyProject.Shared;

namespace MyProject.Unit.Tests.Shared;

public class ErrorTypeTests
{
    [Fact]
    public void Validation_ShouldHaveValue0()
    {
        Assert.Equal(0, (int)ErrorType.Validation);
    }

    [Fact]
    public void Unauthorized_ShouldHaveValue1()
    {
        Assert.Equal(1, (int)ErrorType.Unauthorized);
    }

    [Fact]
    public void NotFound_ShouldHaveValue2()
    {
        Assert.Equal(2, (int)ErrorType.NotFound);
    }

    [Fact]
    public void ShouldHaveExactlyThreeValues()
    {
        var values = Enum.GetValues<ErrorType>();

        Assert.Equal(3, values.Length);
    }
}
