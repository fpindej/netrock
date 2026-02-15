using MyProject.Shared;

namespace MyProject.Unit.Tests.Shared;

public class PhoneNumberHelperTests
{
    [Fact]
    public void Normalize_Null_ShouldReturnNull()
    {
        Assert.Null(PhoneNumberHelper.Normalize(null));
    }

    [Fact]
    public void Normalize_Empty_ShouldReturnNull()
    {
        Assert.Null(PhoneNumberHelper.Normalize(""));
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Normalize_Whitespace_ShouldReturnNull(string input)
    {
        Assert.Null(PhoneNumberHelper.Normalize(input));
    }

    [Fact]
    public void Normalize_AlreadyNormalized_ShouldReturnUnchanged()
    {
        Assert.Equal("+420123456789", PhoneNumberHelper.Normalize("+420123456789"));
    }

    [Fact]
    public void Normalize_ShouldStripSpaces()
    {
        Assert.Equal("+420123456789", PhoneNumberHelper.Normalize("+420 123 456 789"));
    }

    [Fact]
    public void Normalize_ShouldStripDashes()
    {
        Assert.Equal("+420123456789", PhoneNumberHelper.Normalize("+420-123-456-789"));
    }

    [Fact]
    public void Normalize_ShouldStripParentheses()
    {
        Assert.Equal("+1234567890", PhoneNumberHelper.Normalize("+1 (234) 567-890"));
    }

    [Fact]
    public void Normalize_ShouldPreserveLeadingPlus()
    {
        Assert.Equal("+123", PhoneNumberHelper.Normalize("+123"));
    }

    [Fact]
    public void Normalize_ShouldRemoveNonLeadingPlus()
    {
        Assert.Equal("+123456", PhoneNumberHelper.Normalize("+123+456"));
    }

    [Fact]
    public void Normalize_WithoutLeadingPlus_ShouldReturnDigitsOnly()
    {
        Assert.Equal("420123456789", PhoneNumberHelper.Normalize("420 123 456 789"));
    }

    [Fact]
    public void Normalize_MixedCharacters_ShouldStripAll()
    {
        Assert.Equal("+420123456789", PhoneNumberHelper.Normalize("+420 (123) 456-789"));
    }
}
