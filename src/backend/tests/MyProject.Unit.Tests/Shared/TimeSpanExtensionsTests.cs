using MyProject.Shared;

namespace MyProject.Unit.Tests.Shared;

public class TimeSpanExtensionsTests
{
    [Theory]
    [InlineData(1, "1 minute")]
    [InlineData(30, "30 minutes")]
    [InlineData(59, "59 minutes")]
    public void ToHumanReadable_WholeMinutes_ReturnsMinutes(int minutes, string expected)
    {
        Assert.Equal(expected, TimeSpan.FromMinutes(minutes).ToHumanReadable());
    }

    [Theory]
    [InlineData(1, "1 hour")]
    [InlineData(12, "12 hours")]
    [InlineData(23, "23 hours")]
    public void ToHumanReadable_WholeHours_ReturnsHours(int hours, string expected)
    {
        Assert.Equal(expected, TimeSpan.FromHours(hours).ToHumanReadable());
    }

    [Theory]
    [InlineData(1, "1 day")]
    [InlineData(7, "7 days")]
    [InlineData(30, "30 days")]
    [InlineData(365, "365 days")]
    public void ToHumanReadable_WholeDays_ReturnsDays(int days, string expected)
    {
        Assert.Equal(expected, TimeSpan.FromDays(days).ToHumanReadable());
    }

    [Fact]
    public void ToHumanReadable_NonWholeHours_ReturnsMinutes()
    {
        // 1.5 hours = 90 minutes
        Assert.Equal("90 minutes", TimeSpan.FromHours(1.5).ToHumanReadable());
    }

    [Fact]
    public void ToHumanReadable_FractionalDaysWholeHours_ReturnsHours()
    {
        // 2.5 days = 60 hours
        Assert.Equal("60 hours", TimeSpan.FromDays(2.5).ToHumanReadable());
    }

    [Fact]
    public void ToHumanReadable_NonWholeMinutesOrHours_ReturnsTruncatedMinutes()
    {
        // 2 days + 3 hours + 15 minutes = 3075 minutes
        var lifetime = new TimeSpan(days: 2, hours: 3, minutes: 15, seconds: 0);
        Assert.Equal("3075 minutes", lifetime.ToHumanReadable());
    }

    [Fact]
    public void ToHumanReadable_SubMinute_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TimeSpan.FromSeconds(30).ToHumanReadable());
    }

    [Fact]
    public void ToHumanReadable_Zero_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TimeSpan.Zero.ToHumanReadable());
    }

    [Fact]
    public void ToHumanReadable_Negative_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TimeSpan.FromMinutes(-5).ToHumanReadable());
    }
}
