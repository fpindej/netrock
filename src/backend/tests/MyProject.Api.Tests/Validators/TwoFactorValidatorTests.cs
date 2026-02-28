using FluentValidation.TestHelper;
using MyProject.WebApi.Features.Authentication.Dtos.TwoFactor;

namespace MyProject.Api.Tests.Validators;

public class TwoFactorLoginRequestValidatorTests
{
    private readonly TwoFactorLoginRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_ShouldPassValidation()
    {
        var request = new TwoFactorLoginRequest
        {
            ChallengeToken = "challenge-token-123",
            Code = "123456"
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyChallengeToken_ShouldFail()
    {
        var request = new TwoFactorLoginRequest
        {
            ChallengeToken = "",
            Code = "123456"
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.ChallengeToken);
    }

    [Fact]
    public void EmptyCode_ShouldFail()
    {
        var request = new TwoFactorLoginRequest
        {
            ChallengeToken = "challenge-token",
            Code = ""
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void CodeTooShort_ShouldFail()
    {
        var request = new TwoFactorLoginRequest
        {
            ChallengeToken = "challenge-token",
            Code = "123"
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void CodeTooLong_ShouldFail()
    {
        var request = new TwoFactorLoginRequest
        {
            ChallengeToken = "challenge-token",
            Code = "1234567"
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void CodeNonDigits_ShouldFail()
    {
        var request = new TwoFactorLoginRequest
        {
            ChallengeToken = "challenge-token",
            Code = "abcdef"
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Code)
            .WithErrorMessage("Code must be exactly 6 digits.");
    }
}

public class TwoFactorRecoveryLoginRequestValidatorTests
{
    private readonly TwoFactorRecoveryLoginRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_ShouldPassValidation()
    {
        var request = new TwoFactorRecoveryLoginRequest
        {
            ChallengeToken = "challenge-token-123",
            RecoveryCode = "RECOV-12345"
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyChallengeToken_ShouldFail()
    {
        var request = new TwoFactorRecoveryLoginRequest
        {
            ChallengeToken = "",
            RecoveryCode = "RECOV-12345"
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.ChallengeToken);
    }

    [Fact]
    public void EmptyRecoveryCode_ShouldFail()
    {
        var request = new TwoFactorRecoveryLoginRequest
        {
            ChallengeToken = "challenge-token",
            RecoveryCode = ""
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.RecoveryCode);
    }

    [Fact]
    public void RecoveryCodeTooLong_ShouldFail()
    {
        var request = new TwoFactorRecoveryLoginRequest
        {
            ChallengeToken = "challenge-token",
            RecoveryCode = new string('A', 21)
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.RecoveryCode);
    }
}

public class TwoFactorVerifySetupRequestValidatorTests
{
    private readonly TwoFactorVerifySetupRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_ShouldPassValidation()
    {
        var request = new TwoFactorVerifySetupRequest { Code = "123456" };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyCode_ShouldFail()
    {
        var request = new TwoFactorVerifySetupRequest { Code = "" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void CodeTooShort_ShouldFail()
    {
        var request = new TwoFactorVerifySetupRequest { Code = "123" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void CodeTooLong_ShouldFail()
    {
        var request = new TwoFactorVerifySetupRequest { Code = "1234567" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void CodeNonDigits_ShouldFail()
    {
        var request = new TwoFactorVerifySetupRequest { Code = "abcdef" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Code)
            .WithErrorMessage("Code must be exactly 6 digits.");
    }
}

public class TwoFactorDisableRequestValidatorTests
{
    private readonly TwoFactorDisableRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_ShouldPassValidation()
    {
        var request = new TwoFactorDisableRequest { Password = "Password1!" };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyPassword_ShouldFail()
    {
        var request = new TwoFactorDisableRequest { Password = "" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void PasswordTooLong_ShouldFail()
    {
        var request = new TwoFactorDisableRequest { Password = new string('a', 256) };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Password);
    }
}

public class TwoFactorRegenerateCodesRequestValidatorTests
{
    private readonly TwoFactorRegenerateCodesRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_ShouldPassValidation()
    {
        var request = new TwoFactorRegenerateCodesRequest { Password = "Password1!" };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyPassword_ShouldFail()
    {
        var request = new TwoFactorRegenerateCodesRequest { Password = "" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void PasswordTooLong_ShouldFail()
    {
        var request = new TwoFactorRegenerateCodesRequest { Password = new string('a', 256) };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Password);
    }
}
