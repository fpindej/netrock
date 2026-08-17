using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Microsoft.Extensions.Logging;
using MyProject.Application.Features.Email;
using MyProject.Infrastructure.Features.Email.Jobs;
using MyProject.Infrastructure.Features.Email.Services;

namespace MyProject.Component.Tests.Services;

public class BackgroundEmailServiceTests
{
    private readonly IBackgroundJobClient _backgroundJobClient = Substitute.For<IBackgroundJobClient>();
    private readonly BackgroundEmailService _sut;

    public BackgroundEmailServiceTests()
    {
        _sut = new BackgroundEmailService(_backgroundJobClient, Substitute.For<ILogger<BackgroundEmailService>>());
    }

    private static EmailMessage CreateMessage() =>
        new("user@test.com", "Subject", "<html>body</html>", "plain text");

    [Fact]
    public async Task SendEmailAsync_EnqueuesEmailDeliveryJobWithMessage()
    {
        var message = CreateMessage();

        await _sut.SendEmailAsync(message, CancellationToken.None);

        _backgroundJobClient.Received(1).Create(
            Arg.Is<Job>(job =>
                job.Type == typeof(EmailDeliveryJob) &&
                job.Method.Name == nameof(EmailDeliveryJob.ExecuteAsync) &&
                ReferenceEquals(job.Args[0], message)),
            Arg.Any<EnqueuedState>());
    }

    [Fact]
    public async Task SendEmailAsync_DoesNotSendInline()
    {
        await _sut.SendEmailAsync(CreateMessage(), CancellationToken.None);

        // Only the job creation call may hit the client - nothing else is invoked synchronously.
        Assert.Single(_backgroundJobClient.ReceivedCalls());
    }

    [Fact]
    public async Task SendEmailAsync_CancellationRequested_ThrowsAndDoesNotEnqueue()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _sut.SendEmailAsync(CreateMessage(), cts.Token));

        _backgroundJobClient.DidNotReceiveWithAnyArgs().Create(default!, default!);
    }

    [Fact]
    public async Task SendEmailAsync_EnqueuedJob_RoundTripsThroughHangfireSerialization()
    {
        var message = CreateMessage();
        Job? capturedJob = null;
        _backgroundJobClient.Create(Arg.Do<Job>(job => capturedJob = job), Arg.Any<IState>());

        await _sut.SendEmailAsync(message, CancellationToken.None);

        Assert.NotNull(capturedJob);
        var deserialized = InvocationData.SerializeJob(capturedJob).DeserializeJob();

        Assert.Equal(typeof(EmailDeliveryJob), deserialized.Type);
        Assert.Equal(message, Assert.IsType<EmailMessage>(deserialized.Args[0]));
        Assert.Equal(CancellationToken.None, deserialized.Args[1]);
    }
}
