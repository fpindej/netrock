using Microsoft.Extensions.Logging;
using MyProject.Application.Features.Email;
using MyProject.Application.Features.Email.Models;
using MyProject.Infrastructure.Features.Email.Services;

namespace MyProject.Component.Tests.Services;

public class TemplatedEmailSenderTests
{
    private readonly IEmailTemplateRenderer _emailTemplateRenderer;
    private readonly IEmailService _emailService;
    private readonly TemplatedEmailSender _sut;

    public TemplatedEmailSenderTests()
    {
        _emailTemplateRenderer = Substitute.For<IEmailTemplateRenderer>();
        _emailService = Substitute.For<IEmailService>();
        var logger = Substitute.For<ILogger<TemplatedEmailSender>>();

        _sut = new TemplatedEmailSender(_emailTemplateRenderer, _emailService, logger);
    }

    [Fact]
    public async Task SendSafeAsync_RendersAndSendsEmail()
    {
        var rendered = new RenderedEmail("Subject", "<html>body</html>", "plain text");
        _emailTemplateRenderer.Render("test-template", Arg.Any<VerifyEmailModel>())
            .Returns(rendered);

        await _sut.SendSafeAsync("test-template", new VerifyEmailModel("https://example.com"), "user@test.com", CancellationToken.None);

        await _emailService.Received(1).SendEmailAsync(
            Arg.Is<EmailMessage>(m =>
                m.To == "user@test.com" &&
                m.Subject == "Subject" &&
                m.HtmlBody == "<html>body</html>" &&
                m.PlainTextBody == "plain text"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendSafeAsync_RenderThrows_SwallowsException()
    {
        _emailTemplateRenderer.Render<object>(Arg.Any<string>(), Arg.Any<object>())
            .ReturnsForAnyArgs<RenderedEmail>(_ => throw new InvalidOperationException("Template parse error"));

        var exception = await Record.ExceptionAsync(() =>
            _sut.SendSafeAsync("bad-template", new VerifyEmailModel("https://example.com"), "user@test.com", CancellationToken.None));

        Assert.Null(exception);
        await _emailService.DidNotReceiveWithAnyArgs().SendEmailAsync(default!, default);
    }

    [Fact]
    public async Task SendSafeAsync_SendThrows_SwallowsException()
    {
        var rendered = new RenderedEmail("Subject", "<html>body</html>");
        _emailTemplateRenderer.Render("test-template", Arg.Any<VerifyEmailModel>())
            .Returns(rendered);
        _emailService.SendEmailAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new HttpRequestException("SMTP unavailable")));

        var exception = await Record.ExceptionAsync(() =>
            _sut.SendSafeAsync("test-template", new VerifyEmailModel("https://example.com"), "user@test.com", CancellationToken.None));

        Assert.Null(exception);
    }

    [Fact]
    public async Task SendSafeAsync_CancellationRequested_PropagatesCancellation()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var rendered = new RenderedEmail("Subject", "<html>body</html>");
        _emailTemplateRenderer.Render("test-template", Arg.Any<VerifyEmailModel>())
            .Returns(rendered);
        _emailService.SendEmailAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromCanceled(cts.Token));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _sut.SendSafeAsync("test-template", new VerifyEmailModel("https://example.com"), "user@test.com", cts.Token));
    }
}
