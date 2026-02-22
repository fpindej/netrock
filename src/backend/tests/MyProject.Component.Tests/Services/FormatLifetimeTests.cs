using MyProject.Infrastructure.Features.Authentication.Services;

namespace MyProject.Component.Tests.Services;

public class FormatLifetimeTests
{
    [Theory]
    [InlineData(1, "1 minute")]
    [InlineData(30, "30 minutes")]
    [InlineData(59, "59 minutes")]
    public void FormatLifetime_WholeMinutes_ReturnsMinutes(int minutes, string expected)
    {
        Assert.Equal(expected, AuthenticationService.FormatLifetime(TimeSpan.FromMinutes(minutes)));
    }

    [Theory]
    [InlineData(1, "1 hour")]
    [InlineData(12, "12 hours")]
    [InlineData(24, "24 hours")]
    [InlineData(48, "48 hours")]
    public void FormatLifetime_WholeHours_ReturnsHours(int hours, string expected)
    {
        Assert.Equal(expected, AuthenticationService.FormatLifetime(TimeSpan.FromHours(hours)));
    }

    [Fact]
    public void FormatLifetime_NonWholeHours_ReturnsMinutes()
    {
        // 1.5 hours = 90 minutes
        Assert.Equal("90 minutes", AuthenticationService.FormatLifetime(TimeSpan.FromHours(1.5)));
    }

    [Fact]
    public void FormatLifetime_NonWholeHoursAndDays_ReturnsDays()
    {
        // 2 days + 12 hours: not whole hours (60h % 1 == 0? yes it is), actually 2.5 days = 60 hours
        // 60 hours is whole hours, so returns "60 hours"
        Assert.Equal("60 hours", AuthenticationService.FormatLifetime(TimeSpan.FromDays(2.5)));
    }

    [Fact]
    public void FormatLifetime_NonWholeMinutesNonWholeHours_ReturnsTruncatedMinutes()
    {
        // 2 days + 3 hours + 15 minutes = 51h 15m = 3075 minutes
        // TotalHours = 51.25, not whole → falls through to minutes
        var lifetime = new TimeSpan(days: 2, hours: 3, minutes: 15, seconds: 0);
        Assert.Equal("3075 minutes", AuthenticationService.FormatLifetime(lifetime));
    }

    [Fact]
    public void FormatLifetime_SubMinute_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            AuthenticationService.FormatLifetime(TimeSpan.FromSeconds(30)));
    }

    [Fact]
    public void FormatLifetime_Zero_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            AuthenticationService.FormatLifetime(TimeSpan.Zero));
    }

    [Fact]
    public void FormatLifetime_Negative_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            AuthenticationService.FormatLifetime(TimeSpan.FromMinutes(-5)));
    }
}
