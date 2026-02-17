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
            Token = "valid-token"
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyToken_ShouldFail()
    {
        var request = new VerifyEmailRequest { Token = "" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Token);
    }
}
