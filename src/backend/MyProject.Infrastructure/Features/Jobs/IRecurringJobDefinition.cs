namespace MyProject.Infrastructure.Features.Jobs;

/// <summary>
/// Defines a recurring background job that Hangfire will schedule at startup.
/// <para>
/// To add a new recurring job:
/// <list type="number">
/// <item>Create a class implementing this interface.</item>
/// <item>Register it as a scoped service in the DI container.</item>
/// <item>The job will be automatically discovered and registered by <c>UseJobScheduling()</c>.</item>
/// </list>
/// </para>
/// <para>
/// Example:
/// <code>
/// internal sealed class MyCleanupJob(ILogger&lt;MyCleanupJob&gt; logger) : IRecurringJobDefinition
/// {
///     public string JobId => "my-cleanup";
///     public string CronExpression => Cron.Daily();
///     public async Task ExecuteAsync() { /* ... */ }
/// }
/// </code>
/// </para>
/// </summary>
public interface IRecurringJobDefinition
{
    /// <summary>
    /// Gets the unique identifier for this recurring job (e.g. "expired-refresh-token-cleanup").
    /// This ID is used to reference the job in the Hangfire dashboard and admin API.
    /// </summary>
    string JobId { get; }

    /// <summary>
    /// Gets the cron expression that defines the job schedule.
    /// Use <see cref="Hangfire.Cron"/> helper methods (e.g. <c>Cron.Hourly()</c>, <c>Cron.Daily()</c>).
    /// </summary>
    string CronExpression { get; }

    /// <summary>
    /// Executes the job logic. Called by Hangfire on schedule or when manually triggered.
    /// </summary>
    Task ExecuteAsync();
}
