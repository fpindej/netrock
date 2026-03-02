using FluentValidation.TestHelper;
using MyProject.WebApi.Features.Admin.Dtos.DisableTwoFactor;

namespace MyProject.Api.Tests.Validators;

public class DisableTwoFactorRequestValidatorTests
{
    private readonly DisableTwoFactorRequestValidator _validator = new();

    [Fact]
    public void NullReason_ShouldPass() =>
        _validator.TestValidate(new DisableTwoFactorRequest { Reason = null })
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void EmptyReason_ShouldPass() =>
        _validator.TestValidate(new DisableTwoFactorRequest { Reason = "" })
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void ReasonWithinLimit_ShouldPass() =>
        _validator.TestValidate(new DisableTwoFactorRequest { Reason = "Lost authenticator device" })
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void ReasonAtLimit_ShouldPass() =>
        _validator.TestValidate(new DisableTwoFactorRequest { Reason = new string('a', 500) })
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void ReasonOverLimit_ShouldFail() =>
        _validator.TestValidate(new DisableTwoFactorRequest { Reason = new string('a', 501) })
            .ShouldHaveValidationErrorFor(x => x.Reason);
}
