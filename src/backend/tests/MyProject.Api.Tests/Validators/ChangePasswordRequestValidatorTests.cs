using FluentValidation.TestHelper;
using MyProject.WebApi.Features.Authentication.Dtos.ChangePassword;

namespace MyProject.Api.Tests.Validators;

public class ChangePasswordRequestValidatorTests
{
    private readonly ChangePasswordRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_ShouldPassValidation()
    {
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "OldPass1!",
            NewPassword = "NewPass1!"
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyCurrentPassword_ShouldFail()
    {
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "",
            NewPassword = "NewPass1!"
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.CurrentPassword);
    }

    [Fact]
    public void EmptyNewPassword_ShouldFail()
    {
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "OldPass1!",
            NewPassword = ""
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void NewPasswordTooShort_ShouldFail()
    {
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "OldPass1!",
            NewPassword = "Np1"
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void NewPasswordNoLowercase_ShouldFail()
    {
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "OldPass1!",
            NewPassword = "NEWPASS1!"
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("New password must contain at least one lowercase letter.");
    }

    [Fact]
    public void NewPasswordNoUppercase_ShouldFail()
    {
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "OldPass1!",
            NewPassword = "newpass1!"
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("New password must contain at least one uppercase letter.");
    }

    [Fact]
    public void NewPasswordNoDigit_ShouldFail()
    {
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "OldPass1!",
            NewPassword = "NewPasss!"
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("New password must contain at least one digit.");
    }

    [Fact]
    public void NewPasswordSameAsCurrent_ShouldFail()
    {
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "SamePass1!",
            NewPassword = "SamePass1!"
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("New password must be different from the current password.");
    }
}
