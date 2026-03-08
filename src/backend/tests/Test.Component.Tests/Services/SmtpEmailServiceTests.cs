using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Test.Application.Features.Email;
using Test.Infrastructure.Features.Email.Options;
using Test.Infrastructure.Features.Email.Services;

namespace Test.Component.Tests.Services;

public class SmtpEmailServiceTests
{
    private static SmtpEmailService CreateService(EmailOptions? options = null)
    {
        var opts = Options.Create(options ?? new EmailOptions
        {
            Enabled = true,
            Smtp = new EmailOptions.SmtpOptions
            {
                Host = "invalid.host.test",
                Port = 1025,
                UseSsl = false
            }
        });

        return new SmtpEmailService(opts, Substitute.For<ILogger<SmtpEmailService>>());
    }

    private static EmailMessage CreateMessage() =>
        new("test@example.com", "Test Subject", "<p>Hello</p>");

    #region SendEmailAsync

    [Fact]
    public async Task SendEmailAsync_InvalidHost_ThrowsOnConnect()
    {
        var service = CreateService();

        await Assert.ThrowsAnyAsync<Exception>(
            () => service.SendEmailAsync(CreateMessage()));
    }

    [Fact]
    public async Task SendEmailAsync_CancelledToken_ThrowsOperationCancelled()
    {
        var service = CreateService();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.SendEmailAsync(CreateMessage(), cts.Token));
    }

    #endregion
}
