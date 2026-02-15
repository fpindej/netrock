using FluentValidation.TestHelper;
using MyProject.WebApi.Features.Authentication.Dtos.Register;

namespace MyProject.Api.Tests.Validators;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_ShouldPassValidation()
    {
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "Password1"
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyEmail_ShouldFail()
    {
        var request = new RegisterRequest { Email = "", Password = "Password1" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void InvalidEmailFormat_ShouldFail()
    {
        var request = new RegisterRequest { Email = "not-an-email", Password = "Password1" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void EmailTooLong_ShouldFail()
    {
        var request = new RegisterRequest
        {
            Email = new string('a', 250) + "@x.com",
            Password = "Password1"
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void EmptyPassword_ShouldFail()
    {
        var request = new RegisterRequest { Email = "test@example.com", Password = "" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void PasswordTooShort_ShouldFail()
    {
        var request = new RegisterRequest { Email = "test@example.com", Password = "Pa1" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void PasswordNoLowercase_ShouldFail()
    {
        var request = new RegisterRequest { Email = "test@example.com", Password = "PASSWORD1" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must contain at least one lowercase letter.");
    }

    [Fact]
    public void PasswordNoUppercase_ShouldFail()
    {
        var request = new RegisterRequest { Email = "test@example.com", Password = "password1" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must contain at least one uppercase letter.");
    }

    [Fact]
    public void PasswordNoDigit_ShouldFail()
    {
        var request = new RegisterRequest { Email = "test@example.com", Password = "Passwordd" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must contain at least one digit.");
    }

    [Fact]
    public void ValidPhoneNumber_ShouldPass()
    {
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "Password1",
            PhoneNumber = "+420123456789"
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.PhoneNumber);
    }

    [Fact]
    public void NullPhoneNumber_ShouldPass()
    {
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "Password1",
            PhoneNumber = null
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.PhoneNumber);
    }
}
