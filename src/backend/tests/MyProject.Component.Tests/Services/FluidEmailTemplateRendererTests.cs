using Microsoft.Extensions.Options;
using MyProject.Application.Features.Email.Models;
using MyProject.Infrastructure.Features.Email.Options;
using MyProject.Infrastructure.Features.Email.Services;

namespace MyProject.Component.Tests.Services;

public class FluidEmailTemplateRendererTests
{
    private readonly FluidEmailTemplateRenderer _sut;

    public FluidEmailTemplateRendererTests()
    {
        var emailOptions = Options.Create(new EmailOptions
        {
            FromName = "TestApp",
            FrontendBaseUrl = "https://test.example.com"
        });
        _sut = new FluidEmailTemplateRenderer(emailOptions);
    }

    #region VerifyEmail

    [Fact]
    public void VerifyEmail_RendersSubject()
    {
        var model = new VerifyEmailModel("https://example.com/verify?token=abc");

        var result = _sut.Render("verify-email", model);

        Assert.Equal("Verify Your Email Address", result.Subject);
    }

    [Fact]
    public void VerifyEmail_HtmlContainsVerifyUrl()
    {
        var model = new VerifyEmailModel("https://example.com/verify?token=abc");

        var result = _sut.Render("verify-email", model);

        Assert.Contains("https://example.com/verify?token=abc", result.HtmlBody);
    }

    [Fact]
    public void VerifyEmail_HtmlContainsButtonText()
    {
        var model = new VerifyEmailModel("https://example.com/verify?token=abc");

        var result = _sut.Render("verify-email", model);

        Assert.Contains("Verify Email", result.HtmlBody);
    }

    [Fact]
    public void VerifyEmail_PlainTextContainsVerifyUrl()
    {
        var model = new VerifyEmailModel("https://example.com/verify?token=abc");

        var result = _sut.Render("verify-email", model);

        Assert.NotNull(result.PlainTextBody);
        Assert.Contains("https://example.com/verify?token=abc", result.PlainTextBody);
    }

    #endregion

    #region ResetPassword

    [Fact]
    public void ResetPassword_RendersSubject()
    {
        var model = new ResetPasswordModel("https://example.com/reset?token=abc", "24 hours");

        var result = _sut.Render("reset-password", model);

        Assert.Equal("Reset Your Password", result.Subject);
    }

    [Fact]
    public void ResetPassword_HtmlContainsResetUrlAndExpiration()
    {
        var model = new ResetPasswordModel("https://example.com/reset?token=abc", "24 hours");

        var result = _sut.Render("reset-password", model);

        Assert.Contains("https://example.com/reset?token=abc", result.HtmlBody);
        Assert.Contains("24 hours", result.HtmlBody);
    }

    [Fact]
    public void ResetPassword_PlainTextContainsResetUrlAndExpiration()
    {
        var model = new ResetPasswordModel("https://example.com/reset?token=abc", "24 hours");

        var result = _sut.Render("reset-password", model);

        Assert.NotNull(result.PlainTextBody);
        Assert.Contains("https://example.com/reset?token=abc", result.PlainTextBody);
        Assert.Contains("24 hours", result.PlainTextBody);
    }

    #endregion

    #region AdminResetPassword

    [Fact]
    public void AdminResetPassword_RendersSubject()
    {
        var model = new AdminResetPasswordModel("https://example.com/reset?token=abc", "24 hours");

        var result = _sut.Render("admin-reset-password", model);

        Assert.Equal("Reset Your Password", result.Subject);
    }

    [Fact]
    public void AdminResetPassword_HtmlContainsAdminSpecificText()
    {
        var model = new AdminResetPasswordModel("https://example.com/reset?token=abc", "24 hours");

        var result = _sut.Render("admin-reset-password", model);

        Assert.Contains("administrator", result.HtmlBody);
        Assert.Contains("https://example.com/reset?token=abc", result.HtmlBody);
    }

    [Fact]
    public void AdminResetPassword_PlainTextContainsAdminSpecificText()
    {
        var model = new AdminResetPasswordModel("https://example.com/reset?token=abc", "24 hours");

        var result = _sut.Render("admin-reset-password", model);

        Assert.NotNull(result.PlainTextBody);
        Assert.Contains("administrator", result.PlainTextBody);
    }

    #endregion

    #region Invitation

    [Fact]
    public void Invitation_RendersSubject()
    {
        var model = new InvitationModel("https://example.com/reset?token=abc&invited=1", "24 hours");

        var result = _sut.Render("invitation", model);

        Assert.Equal("You've Been Invited", result.Subject);
    }

    [Fact]
    public void Invitation_HtmlContainsSetPasswordUrl()
    {
        var model = new InvitationModel("https://example.com/reset?token=abc&invited=1", "24 hours");

        var result = _sut.Render("invitation", model);

        Assert.Contains("https://example.com/reset?token=abc&amp;invited=1", result.HtmlBody);
    }

    [Fact]
    public void Invitation_PlainTextContainsRawUrl()
    {
        var model = new InvitationModel("https://example.com/reset?token=abc&invited=1", "24 hours");

        var result = _sut.Render("invitation", model);

        Assert.NotNull(result.PlainTextBody);
        Assert.Contains("https://example.com/reset?token=abc&invited=1", result.PlainTextBody);
    }

    #endregion

    #region Layout

    [Fact]
    public void AllTemplates_HtmlWrappedInBaseLayout()
    {
        var model = new VerifyEmailModel("https://example.com/verify?token=abc");

        var result = _sut.Render("verify-email", model);

        Assert.Contains("<!DOCTYPE html>", result.HtmlBody);
        Assert.Contains("TestApp", result.HtmlBody);
    }

    [Fact]
    public void AppName_AppearsInLayoutHeader()
    {
        var model = new ResetPasswordModel("https://example.com/reset?token=abc", "24 hours");

        var result = _sut.Render("reset-password", model);

        Assert.Contains("TestApp", result.HtmlBody);
    }

    #endregion

    #region HtmlEncoding

    [Fact]
    public void HtmlTemplate_EncodesAmpersandInUrl()
    {
        var model = new InvitationModel("https://example.com/reset?token=abc&invited=1", "24 hours");

        var result = _sut.Render("invitation", model);

        Assert.Contains("&amp;", result.HtmlBody);
    }

    [Fact]
    public void PlainTextTemplate_DoesNotEncodeAmpersandInUrl()
    {
        var model = new InvitationModel("https://example.com/reset?token=abc&invited=1", "24 hours");

        var result = _sut.Render("invitation", model);

        Assert.NotNull(result.PlainTextBody);
        Assert.DoesNotContain("&amp;", result.PlainTextBody);
        Assert.Contains("&invited=1", result.PlainTextBody);
    }

    #endregion

    #region ErrorHandling

    [Fact]
    public void MissingTemplate_ThrowsFileNotFoundException()
    {
        var model = new VerifyEmailModel("https://example.com/verify?token=abc");

        Assert.Throws<FileNotFoundException>(() => _sut.Render("nonexistent-template", model));
    }

    #endregion
}
