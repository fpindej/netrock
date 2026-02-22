namespace MyProject.Shared;

/// <summary>
/// Extension methods for formatting <see cref="TimeSpan"/> values as human-readable strings.
/// </summary>
public static class TimeSpanExtensions
{
    extension(TimeSpan timeSpan)
    {
        /// <summary>
        /// Formats the <see cref="TimeSpan"/> as a human-readable duration string using the largest whole unit
        /// that evenly divides the value: days → hours → minutes. Falls back to minutes for fractional values.
        /// </summary>
        /// <returns>A string such as "7 days", "24 hours", "1 minute", or "90 minutes".</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is less than 1 minute.</exception>
        public string ToHumanReadable()
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(timeSpan, TimeSpan.FromMinutes(1));

            if (timeSpan.TotalDays >= 1 && timeSpan.TotalDays % 1 == 0)
            {
                var days = (int)timeSpan.TotalDays;
                return days == 1 ? "1 day" : $"{days} days";
            }

            if (timeSpan.TotalHours >= 1 && timeSpan.TotalHours % 1 == 0)
            {
                var hours = (int)timeSpan.TotalHours;
                return hours == 1 ? "1 hour" : $"{hours} hours";
            }

            var minutes = (int)timeSpan.TotalMinutes;
            return minutes == 1 ? "1 minute" : $"{minutes} minutes";
        }
    }
}
