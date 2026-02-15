using FluentValidation.TestHelper;
using MyProject.WebApi.Features.Authentication.Dtos.Login;

namespace MyProject.Api.Tests.Validators;

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_ShouldPassValidation()
    {
        var request = new LoginRequest
        {
            Username = "test@example.com",
            Password = "Password1"
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyUsername_ShouldFail()
    {
        var request = new LoginRequest { Username = "", Password = "Password1" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public void InvalidEmailFormat_ShouldFail()
    {
        var request = new LoginRequest { Username = "not-email", Password = "Password1" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public void EmptyPassword_ShouldFail()
    {
        var request = new LoginRequest { Username = "test@example.com", Password = "" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void UsernameTooLong_ShouldFail()
    {
        var request = new LoginRequest
        {
            Username = new string('a', 250) + "@x.com",
            Password = "Password1"
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Username);
    }
}
