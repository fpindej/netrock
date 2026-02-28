using FluentValidation;

namespace MyProject.WebApi.Features.Authentication.Dtos.TwoFactor;

/// <summary>
/// Validates <see cref="TwoFactorVerifySetupRequest"/> fields at runtime.
/// </summary>
public class TwoFactorVerifySetupRequestValidator : AbstractValidator<TwoFactorVerifySetupRequest>
{
    /// <summary>
    /// Initializes validation rules for 2FA verify setup requests.
    /// </summary>
    public TwoFactorVerifySetupRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .Length(6)
            .Matches(@"^\d{6}$").WithMessage("Code must be exactly 6 digits.");
    }
}
