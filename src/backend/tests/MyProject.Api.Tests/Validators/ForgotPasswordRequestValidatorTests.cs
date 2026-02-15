using FluentValidation.TestHelper;
using MyProject.WebApi.Features.Authentication.Dtos.ForgotPassword;

namespace MyProject.Api.Tests.Validators;

public class ForgotPasswordRequestValidatorTests
{
    private readonly ForgotPasswordRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_ShouldPassValidation()
    {
        var request = new ForgotPasswordRequest { Email = "test@example.com" };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyEmail_ShouldFail()
    {
        var request = new ForgotPasswordRequest { Email = "" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void InvalidEmailFormat_ShouldFail()
    {
        var request = new ForgotPasswordRequest { Email = "not-an-email" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void EmailTooLong_ShouldFail()
    {
        var request = new ForgotPasswordRequest { Email = new string('a', 250) + "@x.com" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }
}
