using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyProject.Infrastructure.Persistence;

namespace MyProject.Infrastructure.Features.Jobs.RecurringJobs;

/// <summary>
/// Removes expired and consumed two-factor challenge tokens from the database.
/// Runs hourly to keep the TwoFactorChallenges table lean and prevent unbounded growth.
/// </summary>
internal sealed class ExpiredTwoFactorChallengeCleanupJob(
    MyProjectDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<ExpiredTwoFactorChallengeCleanupJob> logger) : IRecurringJobDefinition
{
    /// <inheritdoc />
    public string JobId => "expired-two-factor-challenge-cleanup";

    /// <inheritdoc />
    public string CronExpression => Cron.Hourly();

    /// <summary>
    /// Grace period before deleting expired challenges. Avoids a race condition where a challenge
    /// expires at the exact moment a user submits their TOTP code.
    /// </summary>
    private static readonly TimeSpan ExpirationGracePeriod = TimeSpan.FromHours(1);

    /// <inheritdoc />
    public async Task ExecuteAsync()
    {
        var cutoff = timeProvider.GetUtcNow().UtcDateTime - ExpirationGracePeriod;

        var deletedCount = await dbContext.TwoFactorChallenges
            .Where(c => c.ExpiresAt < cutoff || c.IsUsed)
            .ExecuteDeleteAsync();

        logger.LogInformation("Deleted {Count} expired two-factor challenges", deletedCount);
    }
}
