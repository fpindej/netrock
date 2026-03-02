using FluentValidation.TestHelper;
using MyProject.WebApi.Features.Authentication.Dtos.External;
using MyProject.WebApi.Features.Authentication.Dtos.SetPassword;

namespace MyProject.Api.Tests.Validators;

public class ExternalChallengeRequestValidatorTests
{
    private readonly ExternalChallengeRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_ShouldPassValidation()
    {
        var request = new ExternalChallengeRequest
        {
            Provider = "Google",
            RedirectUri = "https://example.com/oauth/callback"
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyProvider_ShouldFail()
    {
        var request = new ExternalChallengeRequest
        {
            Provider = "",
            RedirectUri = "https://example.com/oauth/callback"
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Provider);
    }

    [Fact]
    public void ProviderTooLong_ShouldFail()
    {
        var request = new ExternalChallengeRequest
        {
            Provider = new string('a', 33),
            RedirectUri = "https://example.com/oauth/callback"
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Provider);
    }

    [Fact]
    public void EmptyRedirectUri_ShouldFail()
    {
        var request = new ExternalChallengeRequest
        {
            Provider = "Google",
            RedirectUri = ""
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.RedirectUri);
    }

    [Fact]
    public void RedirectUriTooLong_ShouldFail()
    {
        var request = new ExternalChallengeRequest
        {
            Provider = "Google",
            RedirectUri = "https://example.com/" + new string('a', 2040)
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.RedirectUri);
    }

    [Fact]
    public void InvalidRedirectUri_ShouldFail()
    {
        var request = new ExternalChallengeRequest
        {
            Provider = "Google",
            RedirectUri = "not-a-uri"
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.RedirectUri);
    }
}

public class ExternalCallbackRequestValidatorTests
{
    private readonly ExternalCallbackRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_ShouldPassValidation()
    {
        var request = new ExternalCallbackRequest
        {
            Code = "auth-code-from-provider",
            State = "state-token-value"
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyCode_ShouldFail()
    {
        var request = new ExternalCallbackRequest
        {
            Code = "",
            State = "state-token-value"
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void CodeTooLong_ShouldFail()
    {
        var request = new ExternalCallbackRequest
        {
            Code = new string('a', 2049),
            State = "state-token-value"
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void EmptyState_ShouldFail()
    {
        var request = new ExternalCallbackRequest
        {
            Code = "auth-code-from-provider",
            State = ""
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.State);
    }

    [Fact]
    public void StateTooLong_ShouldFail()
    {
        var request = new ExternalCallbackRequest
        {
            Code = "auth-code-from-provider",
            State = new string('a', 513)
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.State);
    }
}

public class ExternalUnlinkRequestValidatorTests
{
    private readonly ExternalUnlinkRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_ShouldPassValidation()
    {
        var request = new ExternalUnlinkRequest { Provider = "Google" };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyProvider_ShouldFail()
    {
        var request = new ExternalUnlinkRequest { Provider = "" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Provider);
    }

    [Fact]
    public void ProviderTooLong_ShouldFail()
    {
        var request = new ExternalUnlinkRequest { Provider = new string('a', 33) };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Provider);
    }
}

public class SetPasswordRequestValidatorTests
{
    private readonly SetPasswordRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_ShouldPassValidation()
    {
        var request = new SetPasswordRequest { NewPassword = "Password1a" };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyPassword_ShouldFail()
    {
        var request = new SetPasswordRequest { NewPassword = "" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void PasswordTooShort_ShouldFail()
    {
        var request = new SetPasswordRequest { NewPassword = "Ab1" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void PasswordTooLong_ShouldFail()
    {
        var request = new SetPasswordRequest { NewPassword = "Aa1" + new string('x', 253) };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void PasswordNoLowercase_ShouldFail()
    {
        var request = new SetPasswordRequest { NewPassword = "PASSWORD1" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("Password must contain at least one lowercase letter.");
    }

    [Fact]
    public void PasswordNoUppercase_ShouldFail()
    {
        var request = new SetPasswordRequest { NewPassword = "password1" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("Password must contain at least one uppercase letter.");
    }

    [Fact]
    public void PasswordNoDigit_ShouldFail()
    {
        var request = new SetPasswordRequest { NewPassword = "Passwordx" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("Password must contain at least one digit.");
    }
}
