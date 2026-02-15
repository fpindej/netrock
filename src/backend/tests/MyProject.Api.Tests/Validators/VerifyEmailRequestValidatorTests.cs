using FluentValidation.TestHelper;
using MyProject.WebApi.Features.Authentication.Dtos.VerifyEmail;

namespace MyProject.Api.Tests.Validators;

public class VerifyEmailRequestValidatorTests
{
    private readonly VerifyEmailRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_ShouldPassValidation()
    {
        var request = new VerifyEmailRequest
        {
            Email = "test@example.com",
            Token = "valid-token"
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyEmail_ShouldFail()
    {
        var request = new VerifyEmailRequest { Email = "", Token = "token" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void InvalidEmailFormat_ShouldFail()
    {
        var request = new VerifyEmailRequest { Email = "not-an-email", Token = "token" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void EmptyToken_ShouldFail()
    {
        var request = new VerifyEmailRequest { Email = "test@example.com", Token = "" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Token);
    }
}
