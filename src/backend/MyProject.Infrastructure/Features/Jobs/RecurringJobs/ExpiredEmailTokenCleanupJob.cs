using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyProject.Infrastructure.Persistence;

namespace MyProject.Infrastructure.Features.Jobs.RecurringJobs;

/// <summary>
/// Removes expired and consumed email tokens from the database.
/// Runs hourly to keep the EmailTokens table lean and prevent unbounded growth.
/// </summary>
internal sealed class ExpiredEmailTokenCleanupJob(
    MyProjectDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<ExpiredEmailTokenCleanupJob> logger) : IRecurringJobDefinition
{
    /// <inheritdoc />
    public string JobId => "expired-email-token-cleanup";

    /// <inheritdoc />
    public string CronExpression => Cron.Hourly();

    /// <summary>
    /// Grace period before deleting expired tokens. Avoids a race condition where a token
    /// expires at the exact moment it is being resolved — the auth service has loaded the
    /// token (still valid) but hasn't called SaveChangesAsync yet, while the cleanup job
    /// deletes the now-expired row, causing a DbUpdateConcurrencyException.
    /// </summary>
    private static readonly TimeSpan ExpirationGracePeriod = TimeSpan.FromHours(1);

    /// <inheritdoc />
    public async Task ExecuteAsync()
    {
        var cutoff = timeProvider.GetUtcNow().UtcDateTime - ExpirationGracePeriod;

        var deletedCount = await dbContext.EmailTokens
            .Where(t => t.ExpiresAt < cutoff || t.IsUsed)
            .ExecuteDeleteAsync();

        logger.LogInformation("Deleted {Count} expired email tokens", deletedCount);
    }
}
