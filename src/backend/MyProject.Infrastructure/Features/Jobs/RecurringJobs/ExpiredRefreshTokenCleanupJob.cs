using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyProject.Infrastructure.Persistence;

namespace MyProject.Infrastructure.Features.Jobs.RecurringJobs;

/// <summary>
/// Removes expired and consumed refresh tokens from the database.
/// Runs hourly to keep the RefreshTokens table lean and prevent unbounded growth.
/// </summary>
internal sealed class ExpiredRefreshTokenCleanupJob(
    MyProjectDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<ExpiredRefreshTokenCleanupJob> logger) : IRecurringJobDefinition
{
    /// <inheritdoc />
    public string JobId => "expired-refresh-token-cleanup";

    /// <inheritdoc />
    public string CronExpression => Cron.Hourly();

    /// <inheritdoc />
    public async Task ExecuteAsync()
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var deletedCount = await dbContext.RefreshTokens
            .Where(t => t.ExpiredAt < now && (t.IsUsed || t.IsInvalidated))
            .ExecuteDeleteAsync();

        logger.LogInformation("Deleted {Count} expired refresh tokens", deletedCount);
    }
}
