using FluentValidation.TestHelper;
using MyProject.WebApi.Features.Authentication.Dtos.ResetPassword;

namespace MyProject.Api.Tests.Validators;

public class ResetPasswordRequestValidatorTests
{
    private readonly ResetPasswordRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_ShouldPassValidation()
    {
        var request = new ResetPasswordRequest
        {
            Email = "test@example.com",
            Token = "valid-token",
            NewPassword = "Password1"
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyEmail_ShouldFail()
    {
        var request = new ResetPasswordRequest { Email = "", Token = "token", NewPassword = "Password1" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void InvalidEmailFormat_ShouldFail()
    {
        var request = new ResetPasswordRequest { Email = "not-an-email", Token = "token", NewPassword = "Password1" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void EmptyToken_ShouldFail()
    {
        var request = new ResetPasswordRequest { Email = "test@example.com", Token = "", NewPassword = "Password1" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Token);
    }

    [Fact]
    public void EmptyPassword_ShouldFail()
    {
        var request = new ResetPasswordRequest { Email = "test@example.com", Token = "token", NewPassword = "" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void PasswordTooShort_ShouldFail()
    {
        var request = new ResetPasswordRequest { Email = "test@example.com", Token = "token", NewPassword = "Pa1" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void PasswordNoLowercase_ShouldFail()
    {
        var request = new ResetPasswordRequest { Email = "test@example.com", Token = "token", NewPassword = "PASSWORD1" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("Password must contain at least one lowercase letter.");
    }

    [Fact]
    public void PasswordNoUppercase_ShouldFail()
    {
        var request = new ResetPasswordRequest { Email = "test@example.com", Token = "token", NewPassword = "password1" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("Password must contain at least one uppercase letter.");
    }

    [Fact]
    public void PasswordNoDigit_ShouldFail()
    {
        var request = new ResetPasswordRequest { Email = "test@example.com", Token = "token", NewPassword = "Passwordd" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("Password must contain at least one digit.");
    }
}
