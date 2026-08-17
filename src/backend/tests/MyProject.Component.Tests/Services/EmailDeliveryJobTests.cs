using System.Reflection;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyProject.Application.Features.Email;
using MyProject.Infrastructure.Features.Email.Jobs;
using MyProject.Infrastructure.Features.Email.Options;
using MyProject.Infrastructure.Features.Email.Services;

namespace MyProject.Component.Tests.Services;

public class EmailDeliveryJobTests
{
    private static readonly MethodInfo ExecuteMethod =
        typeof(EmailDeliveryJob).GetMethod(nameof(EmailDeliveryJob.ExecuteAsync))!;

    private static EmailDeliveryJob CreateJob()
    {
        var options = Options.Create(new EmailOptions
        {
            Enabled = true,
            Smtp = new EmailOptions.SmtpOptions
            {
                Host = "invalid.host.test",
                Port = 1025,
                UseSsl = false
            }
        });

        var smtpEmailService = new SmtpEmailService(options, Substitute.For<ILogger<SmtpEmailService>>());
        return new EmailDeliveryJob(smtpEmailService);
    }

    private static EmailMessage CreateMessage() =>
        new("user@test.com", "Subject", "<html>body</html>");

    [Fact]
    public void ExecuteAsync_HasAutomaticRetryWithBackoff()
    {
        var retry = ExecuteMethod.GetCustomAttribute<AutomaticRetryAttribute>();

        Assert.NotNull(retry);
        Assert.Equal(EmailDeliveryJob.RetryAttempts, retry.Attempts);
        Assert.True(retry.Attempts > 0);
        Assert.Equal(retry.Attempts, retry.DelaysInSeconds.Length);
        Assert.Equal(AttemptsExceededAction.Fail, retry.OnAttemptsExceeded);
    }

    [Fact]
    public void ExecuteAsync_RetryDelays_AreIncreasing()
    {
        var delays = ExecuteMethod.GetCustomAttribute<AutomaticRetryAttribute>()!.DelaysInSeconds;

        Assert.All(delays, delay => Assert.True(delay > 0));
        Assert.Equal(delays.OrderBy(d => d), delays);
    }

    [Fact]
    public async Task ExecuteAsync_SmtpFailure_PropagatesExceptionForRetry()
    {
        var job = CreateJob();

        var exception = await Record.ExceptionAsync(() =>
            job.ExecuteAsync(CreateMessage(), CancellationToken.None));

        Assert.NotNull(exception);
        Assert.IsNotType<OperationCanceledException>(exception);
    }

    [Fact]
    public async Task ExecuteAsync_CancelledToken_ThrowsOperationCancelled()
    {
        var job = CreateJob();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            job.ExecuteAsync(CreateMessage(), cts.Token));
    }
}
